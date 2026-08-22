using System.Reflection;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Covers the notification-sound features: Chat panel's own "sound on new message"
/// setting (see <see cref="MainWindowViewModel.ChatSoundOnNewMessageEnabled"/>), a trigger's
/// own optional "play sound on match" (see <see cref="AutomationRuleEntry.PlaySoundOnMatch"/>),
/// and a timer's own optional "play sound on tick" (see <see cref="TimerEntry.PlaySoundOnTick"/>).
/// All three are wired through <see cref="MainWindowViewModel.PlayNotificationSound"/> — overridden
/// here to a no-op counter instead of the real Windows system beep, avoiding an audible side
/// effect during test runs (same "overridable in tests" pattern as
/// PanelToolView.ConfirmDeletionAsync). TriggerEngine's own RuleMatched event and the
/// PlaySoundOnMatch flag propagation are covered at the pure-logic level by TriggerEngineTests in
/// MudClient.Core.Tests; this file covers the ViewModel wiring end-to-end.</summary>
public sealed class ChatAndTriggerSoundTests : IAsyncDisposable
{
    private readonly string _tempDir;
    private readonly MainWindowViewModel _vm;
    private int _soundPlayCount;

    public ChatAndTriggerSoundTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "KillerMudClient_SoundTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _vm = new MainWindowViewModel(
            profileService: new ProfileService(Path.Combine(_tempDir, "Profiles")),
            settingsService: new AppSettingsService(_tempDir),
            groupSpellStore: new GroupSpellStore(Path.Combine(_tempDir, "group-spells.json")));
        _vm.PlayNotificationSound = () => _soundPlayCount++;
    }

    public async ValueTask DisposeAsync()
    {
        await _vm.DisposeAsync();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private static void InvokeOnLineReceived(MainWindowViewModel vm, string line) =>
        typeof(MainWindowViewModel)
            .GetMethod("OnLineReceived", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(vm, [line]);

    private static void InvokeApplyAutomation(MainWindowViewModel vm) =>
        typeof(MainWindowViewModel)
            .GetMethod("ApplyAutomation", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(vm, null);

    // ====================================================================
    // Chat panel's "sound on new message" setting
    // ====================================================================

    [Fact]
    public void ChatLine_SoundDisabledByDefault_DoesNotPlay()
    {
        Assert.False(_vm.ChatSoundOnNewMessageEnabled);

        InvokeOnLineReceived(_vm, "Gandalf mowi: 'Witaj.'");

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void ChatLine_SoundEnabled_Plays()
    {
        _vm.ChatSoundOnNewMessageEnabled = true;

        InvokeOnLineReceived(_vm, "Gandalf mowi: 'Witaj.'");

        Assert.Equal(1, _soundPlayCount);
    }

    [Fact]
    public void NonChatLine_SoundEnabled_DoesNotPlay()
    {
        _vm.ChatSoundOnNewMessageEnabled = true;

        InvokeOnLineReceived(_vm, "Ordinary room description text.");

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void ChatSoundOnNewMessageEnabled_Persists()
    {
        _vm.ChatSoundOnNewMessageEnabled = true;

        Assert.True(_vm.ChatSoundOnNewMessageEnabled);
    }

    // ====================================================================
    // Per-trigger "play sound on match"
    // ====================================================================

    [Fact]
    public void TriggerWithSoundEnabled_Matches_Plays()
    {
        _vm.AutomationRules.Add(new AutomationRuleEntry(
            "boss", "trigger", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: true));
        InvokeApplyAutomation(_vm);

        InvokeOnLineReceived(_vm, "Zabijasz golema.");

        Assert.Equal(1, _soundPlayCount);
    }

    [Fact]
    public void TriggerWithSoundDisabled_Matches_DoesNotPlay()
    {
        _vm.AutomationRules.Add(new AutomationRuleEntry(
            "boss", "trigger", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: false));
        InvokeApplyAutomation(_vm);

        InvokeOnLineReceived(_vm, "Zabijasz golema.");

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void TriggerWithSound_NonMatchingLine_DoesNotPlay()
    {
        _vm.AutomationRules.Add(new AutomationRuleEntry(
            "boss", "trigger", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: true));
        InvokeApplyAutomation(_vm);

        InvokeOnLineReceived(_vm, "Nic się nie dzieje.");

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void TriggerSound_FiresIndependentlyOfChatSoundSetting()
    {
        // ChatSoundOnNewMessageEnabled stays at its default (off) — the trigger's own flag is
        // what decides here, not the chat setting.
        Assert.False(_vm.ChatSoundOnNewMessageEnabled);
        _vm.AutomationRules.Add(new AutomationRuleEntry(
            "boss", "trigger", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: true));
        InvokeApplyAutomation(_vm);

        InvokeOnLineReceived(_vm, "Zabijasz golema.");

        Assert.Equal(1, _soundPlayCount);
    }

    [Fact]
    public void AliasWithPlaySoundOnMatchSetTrue_NeverFiresBecauseAliasesDoNotEvaluateAgainstServerLines()
    {
        // PlaySoundOnMatch is meaningful only for triggers (see the type's own doc comment) —
        // an alias fires on typed input via a different engine that OnLineReceived never touches,
        // so setting the flag on one (however it got set) has nothing to hook into here.
        _vm.AutomationRules.Add(new AutomationRuleEntry(
            "a", "alias", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: true));
        InvokeApplyAutomation(_vm);

        InvokeOnLineReceived(_vm, "Zabijasz golema.");

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void EditRule_TogglingSoundOff_StopsPlayingOnSubsequentMatches()
    {
        var rule = new AutomationRuleEntry(
            "boss", "trigger", "Zabijasz golema", "attack", isEnabled: true, playSoundOnMatch: true);
        _vm.AutomationRules.Add(rule);
        InvokeApplyAutomation(_vm);
        InvokeOnLineReceived(_vm, "Zabijasz golema.");
        Assert.Equal(1, _soundPlayCount);

        rule.PlaySoundOnMatch = false;
        InvokeApplyAutomation(_vm);
        InvokeOnLineReceived(_vm, "Zabijasz golema.");

        Assert.Equal(1, _soundPlayCount); // unchanged — no second play
    }

    // ====================================================================
    // Per-timer "play sound on tick" (see TimerEntry.PlaySoundOnTick)
    // ====================================================================

    [Fact]
    public async Task TimerWithSoundEnabled_Fires_PlaysSound()
    {
        _vm.NewTimerName = "Tick";
        _vm.NewTimerMilliseconds = "20";
        _vm.NewTimerCommands = "look";
        _vm.NewTimerPlaySoundOnTick = true;
        _vm.AddTimerCommand.Execute(null);
        var timer = _vm.Timers[^1];

        _vm.ToggleTimerCommand.Execute(timer);
        await WaitUntilAsync(() => _soundPlayCount > 0);

        Assert.True(_soundPlayCount > 0);
    }

    [Fact]
    public async Task TimerWithSoundDisabled_Fires_DoesNotPlay()
    {
        _vm.NewTimerName = "Tick";
        _vm.NewTimerMilliseconds = "20";
        _vm.NewTimerCommands = "look";
        _vm.NewTimerPlaySoundOnTick = false;
        _vm.AddTimerCommand.Execute(null);
        var timer = _vm.Timers[^1];

        _vm.ToggleTimerCommand.Execute(timer);
        await Task.Delay(120, TestContext.Current.CancellationToken);

        Assert.Equal(0, _soundPlayCount);
    }

    [Fact]
    public void EditTimer_LoadsPlaySoundOnTickIntoForm()
    {
        _vm.NewTimerName = "Tick";
        _vm.NewTimerSeconds = "5";
        _vm.NewTimerCommands = "look";
        _vm.NewTimerPlaySoundOnTick = true;
        _vm.AddTimerCommand.Execute(null);
        var timer = _vm.Timers[^1];
        _vm.NewTimerPlaySoundOnTick = false;

        _vm.EditTimerCommand.Execute(timer);

        Assert.True(_vm.NewTimerPlaySoundOnTick);
    }

    [Fact]
    public void CancelTimerEdit_ResetsPlaySoundOnTickToFalse()
    {
        _vm.NewTimerPlaySoundOnTick = true;

        _vm.CancelTimerEditCommand.Execute(null);

        Assert.False(_vm.NewTimerPlaySoundOnTick);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMilliseconds = 2000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            await Task.Delay(10, TestContext.Current.CancellationToken);
        }
    }
}
