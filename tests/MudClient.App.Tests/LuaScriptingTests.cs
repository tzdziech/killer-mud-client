using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Automation;

namespace MudClient.App.Tests;

/// <summary>Covers <see cref="MainWindowViewModel"/>'s Lua scripting wiring: that "script"
/// aliases/triggers/timers (<see cref="AutomationRuleEntry.IsScript"/>/<see cref="TimerEntry.IsScript"/>)
/// actually reach the shared <c>_lua</c> engine with live game state, and that a script error
/// surfaces as a toast instead of crashing anything. The Lua engine's own behavior (send/echo/
/// matches/persistence/errors) is covered directly by LuaScriptEngineTests and
/// AliasEngineTests/TriggerEngineTests in MudClient.Core.Tests — this file only checks the App-side
/// wiring: ApplyAutomation building script rules correctly, BuildLuaGameState reading live fields,
/// and RunScriptTimer/OnLuaScriptError.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class LuaScriptingTests
{
    private static (MainWindowViewModel ViewModel, string Directory) CreateViewModel()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return (new MainWindowViewModel(
            profileService: new ProfileService(Path.Combine(directory, "Profiles")),
            settingsService: new AppSettingsService(directory)), directory);
    }

    private static T GetPrivateField<T>(MainWindowViewModel viewModel, string fieldName) =>
        (T)typeof(MainWindowViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(viewModel)!;

    private static void SetPrivateField(MainWindowViewModel viewModel, string fieldName, object? value) =>
        typeof(MainWindowViewModel).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(viewModel, value);

    private static object InvokePrivate(MainWindowViewModel viewModel, string methodName, params object?[] args) =>
        typeof(MainWindowViewModel).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, args)!;

    [Fact]
    public async Task ApplyAutomation_ScriptAlias_BuildsAScriptAliasRule()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "kk", "alias", "^kk (.+)$", "send(\"kill \" .. matches[1])",
                isEnabled: true, isScript: true));

            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            var rule = Assert.Single(aliases.Rules);
            Assert.True(rule.IsScript);
            Assert.Equal("send(\"kill \" .. matches[1])", rule.Replacement);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyAutomation_ScriptTrigger_BuildsAScriptTriggerRule()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "golem", "trigger", @"^Zabijasz (.+)\.$", "send(\"gratulacje\")",
                isEnabled: true, isScript: true));

            InvokePrivate(viewModel, "ApplyAutomation");

            var triggers = GetPrivateField<TriggerEngine>(viewModel, "_triggers");
            var rule = Assert.Single(triggers.Rules);
            Assert.True(rule.IsScript);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScriptAlias_EndToEnd_ProducesExpectedCommandThroughSharedLuaEngine()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "kk", "alias", "^kk (.+)$", "send(\"kill \" .. matches[1])",
                isEnabled: true, isScript: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            var result = aliases.ProcessCommands("kk orc");

            Assert.Equal(["kill orc"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task BuildLuaGameState_ReflectsLiveHpAndMovementFields()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            SetPrivateField(viewModel, "_latestHp", 42);
            SetPrivateField(viewModel, "_latestMaxHp", 100);
            SetPrivateField(viewModel, "_latestMovement", 10);
            SetPrivateField(viewModel, "_latestMaximumMovement", 50);
            SetPrivateField(viewModel, "_latestCharacterName", "Frodo");
            SetPrivateField(viewModel, "_latestCharacterPosition", "fighting");

            var state = InvokePrivate(viewModel, "BuildLuaGameState");
            var type = state.GetType();

            Assert.Equal(42, type.GetProperty("Hp")!.GetValue(state));
            Assert.Equal(100, type.GetProperty("MaxHp")!.GetValue(state));
            Assert.Equal(10, type.GetProperty("Mv")!.GetValue(state));
            Assert.Equal(50, type.GetProperty("MaxMv")!.GetValue(state));
            Assert.Equal("Frodo", type.GetProperty("CharacterName")!.GetValue(state));
            Assert.Equal("fighting", type.GetProperty("Position")!.GetValue(state));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScriptAlias_UsingGameState_SeesLiveHp()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            SetPrivateField(viewModel, "_latestHp", 30);
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "heal", "alias", "^hh$", "if hp and hp < 50 then send(\"pij miksture\") end",
                isEnabled: true, isScript: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            var result = aliases.ProcessCommands("hh");

            Assert.Equal(["pij miksture"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task OnLuaScriptError_AddsAnErrorToastNamingTheRule()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            InvokePrivate(viewModel, "OnLuaScriptError", "MojaReguła", "coś poszło nie tak");
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(viewModel.Toasts, t =>
                t.Type == "error" && t.Text.Contains("MojaReguła") && t.Text.Contains("coś poszło nie tak"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunScriptTimer_ReturnsSendCommands()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var timer = new TimerEntry
            {
                Name = "t",
                Minutes = 1,
                CommandsText = "send(\"look\")",
                IsScript = true,
            };

            var result = (IReadOnlyList<string>)InvokePrivate(viewModel, "RunScriptTimer", timer);

            Assert.Equal(["look"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task RunScriptTimer_ScriptThrows_ReportsToastAndReturnsEmpty()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var timer = new TimerEntry
            {
                Name = "BadTimer",
                Minutes = 1,
                CommandsText = "error(\"boom\")",
                IsScript = true,
            };

            var result = (IReadOnlyList<string>)InvokePrivate(viewModel, "RunScriptTimer", timer);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(result);
            Assert.Contains(viewModel.Toasts, t => t.Text.Contains("BadTimer") && t.Text.Contains("boom"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task RunScriptTimer_SendResultExpandsThroughAliasCall()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.AutomationRules.Add(new AutomationRuleEntry("mk", "alias", "^mk$", "kill orc\nkill goblin", isEnabled: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var timer = new TimerEntry
            {
                Name = "t",
                Minutes = 1,
                CommandsText = "send(\"alias(mk)\")",
                IsScript = true,
            };

            var result = (IReadOnlyList<string>)InvokePrivate(viewModel, "RunScriptTimer", timer);

            Assert.Equal(["kill orc", "kill goblin"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ScriptRule_PersistsLuaGlobalsAcrossFirings()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "counter", "alias", "^cc$", "count = (count or 0) + 1\nsend(\"count: \" .. count)",
                isEnabled: true, isScript: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            aliases.ProcessCommands("cc");
            aliases.ProcessCommands("cc");
            var result = aliases.ProcessCommands("cc");

            Assert.Equal(["count: 3"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    // ====================================================================
    // Lua library — per-profile shared helper functions (ProfileData.LuaLibrary)
    // ====================================================================

    [Fact]
    public async Task NewProfile_LibraryFunction_AvailableToScriptAliasesImmediately()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.NewProfileName = "Frodo";
            viewModel.CreateProfileCommand.Execute(null);
            viewModel.LuaLibrarySource = "function shout(name) return \"OGŁASZA: \" .. name end";
            viewModel.ApplyLuaLibraryCommand.Execute(null);
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "s", "alias", "^s (.+)$", "send(shout(matches[1]))", isEnabled: true, isScript: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            var result = aliases.ProcessCommands("s smok");

            Assert.Equal(["OGŁASZA: smok"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task ApplyLuaLibraryCommand_PersistsSourceToTheActiveProfile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var service = new ProfileService(Path.Combine(directory, "Profiles"));
        var viewModel = new MainWindowViewModel(profileService: service, settingsService: new AppSettingsService(directory));
        try
        {
            viewModel.NewProfileName = "Frodo";
            viewModel.CreateProfileCommand.Execute(null);
            viewModel.LuaLibrarySource = "function shout() return 1 end";

            viewModel.ApplyLuaLibraryCommand.Execute(null);

            var stored = service.Load("Frodo");
            Assert.Equal("function shout() return 1 end", stored!.LuaLibrary);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SelectingASavedProfile_LoadsItsPersistedLibrary()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var service = new ProfileService(Path.Combine(directory, "Profiles"));
        service.Save(new ProfileData
        {
            Name = "Gimli",
            LuaLibrary = "function shout(name) return \"HEJ: \" .. name end",
        });
        var viewModel = new MainWindowViewModel(profileService: service, settingsService: new AppSettingsService(directory));
        try
        {
            viewModel.SelectedProfileName = "Gimli";
            viewModel.SelectProfileCommand.Execute(null);
            viewModel.AutomationRules.Add(new AutomationRuleEntry(
                "s", "alias", "^s (.+)$", "send(shout(matches[1]))", isEnabled: true, isScript: true));
            InvokePrivate(viewModel, "ApplyAutomation");

            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            var result = aliases.ProcessCommands("s Legolas");

            Assert.Equal(["HEJ: Legolas"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SwitchingProfiles_LuaStateDoesNotLeakBetweenCharacters()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var service = new ProfileService(Path.Combine(directory, "Profiles"));
        service.Save(new ProfileData { Name = "Alpha" });
        service.Save(new ProfileData { Name = "Beta" });
        var viewModel = new MainWindowViewModel(profileService: service, settingsService: new AppSettingsService(directory));
        try
        {
            var counterAlias = new AutomationRuleEntry(
                "counter", "alias", "^cc$", "count = (count or 0) + 1\nsend(\"count: \" .. count)",
                isEnabled: true, isScript: true);

            viewModel.SelectedProfileName = "Alpha";
            viewModel.SelectProfileCommand.Execute(null);
            viewModel.AutomationRules.Add(counterAlias);
            InvokePrivate(viewModel, "ApplyAutomation");
            var aliases = GetPrivateField<AliasEngine>(viewModel, "_aliases");
            aliases.ProcessCommands("cc");
            aliases.ProcessCommands("cc");
            Assert.Equal(["count: 3"], aliases.ProcessCommands("cc"));

            viewModel.SwitchProfileCommand.Execute(null);
            viewModel.SelectedProfileName = "Beta";
            viewModel.SelectProfileCommand.Execute(null);
            viewModel.AutomationRules.Add(counterAlias);
            InvokePrivate(viewModel, "ApplyAutomation");

            // Same LuaScriptEngine instance, but Beta's session must start with fresh globals.
            var result = aliases.ProcessCommands("cc");

            Assert.Equal(["count: 1"], result);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task ApplyLuaLibraryCommand_SyntaxError_ShowsToastButStillSaves()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var service = new ProfileService(Path.Combine(directory, "Profiles"));
        var viewModel = new MainWindowViewModel(profileService: service, settingsService: new AppSettingsService(directory));
        try
        {
            viewModel.NewProfileName = "Frodo";
            viewModel.CreateProfileCommand.Execute(null);
            viewModel.LuaLibrarySource = "function broken(";

            viewModel.ApplyLuaLibraryCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(viewModel.Toasts, t => t.Type == "error" && t.Text.Contains("bibliotece Lua"));
            var stored = service.Load("Frodo");
            Assert.Equal("function broken(", stored!.LuaLibrary);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task SelectingAProfileWithABrokenLibrary_ShowsErrorToastButStillActivates()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_LuaTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var service = new ProfileService(Path.Combine(directory, "Profiles"));
        service.Save(new ProfileData { Name = "Broken", LuaLibrary = "function broken(" });
        var viewModel = new MainWindowViewModel(profileService: service, settingsService: new AppSettingsService(directory));
        try
        {
            viewModel.SelectedProfileName = "Broken";
            viewModel.SelectProfileCommand.Execute(null);
            Dispatcher.UIThread.RunJobs();

            Assert.Equal("Broken", viewModel.ActiveProfileName);
            Assert.Contains(viewModel.Toasts, t => t.Type == "error" && t.Text.Contains("bibliotece Lua"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
