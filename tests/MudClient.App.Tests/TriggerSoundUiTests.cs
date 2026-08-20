using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views.Panels;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Renders AutomationPanelView's shared rule editor (RuleEditorTemplate) to prove the
/// "Odtwórz dźwięk przy dopasowaniu" checkbox added for trigger sound support is actually visible
/// and wired to a real Command-backed Add flow — not just a binding path that happens to compile.
/// See the codebase's own prior lesson (PinnedTabUiTests.OverlayMoveButtons_...) that a compiled
/// binding can still fail to do anything at runtime.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class TriggerSoundUiTests
{
    private static void Pump(Window window, int iterations = 10)
    {
        for (var i = 0; i < iterations; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static (MainWindowViewModel ViewModel, string Directory) CreateViewModel()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_TriggerSoundUiTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return (new MainWindowViewModel(settingsService: new AppSettingsService(directory)), directory);
    }

    [AvaloniaFact]
    public async Task StartAddTrigger_PlaySoundCheckbox_IsVisibleAndSettingItSavesOntoTheNewRule()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var panel = new AutomationPanelView { DataContext = viewModel };
            var window = new Window { Width = 600, Height = 700, Content = panel };
            window.Show();
            Pump(window);

            viewModel.StartAddTriggerCommand.Execute(null);
            Pump(window);

            var checkbox = window.GetVisualDescendants().OfType<CheckBox>()
                .Single(c => Equals(c.Content, "Odtwórz dźwięk przy dopasowaniu"));
            Assert.True(checkbox.IsVisible);

            viewModel.NewRuleName = "boss";
            viewModel.NewRulePattern = "Zabijasz golema";
            viewModel.NewRuleAction = "attack";
            checkbox.IsChecked = true;
            Pump(window);

            Assert.True(viewModel.NewRulePlaySoundOnMatch);

            viewModel.AddRuleCommand.Execute(null);

            var saved = Assert.Single(viewModel.TriggerRules);
            Assert.True(saved.PlaySoundOnMatch);

            window.Close();
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task StartAddAlias_PlaySoundOptionDoesNotApply()
    {
        // The checkbox is meaningless for an alias (it fires on typed input, not server output —
        // see AutomationRuleEntry.PlaySoundOnMatch's own doc comment) — NewRuleIsTrigger, which
        // gates its visibility, must be false while adding one.
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var panel = new AutomationPanelView { DataContext = viewModel };
            var window = new Window { Width = 600, Height = 700, Content = panel };
            window.Show();
            Pump(window);

            viewModel.StartAddAliasCommand.Execute(null);
            Pump(window);

            Assert.False(viewModel.NewRuleIsTrigger);

            viewModel.NewRuleName = "shortcut";
            viewModel.NewRulePattern = "^gg$";
            viewModel.NewRuleAction = "look";
            viewModel.AddRuleCommand.Execute(null);

            var saved = Assert.Single(viewModel.AliasRules);
            Assert.False(saved.PlaySoundOnMatch);

            window.Close();
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
