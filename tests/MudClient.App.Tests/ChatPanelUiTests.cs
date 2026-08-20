using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MudClient.App.Controls;
using MudClient.App.Docking;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views;
using MudClient.App.Views.Panels;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Renders the Chat panel's own settings button (docked, via PanelToolView) to prove the
/// "sound on new message" checkbox is actually visible and its binding actually flips
/// MainWindowViewModel.ChatSoundOnNewMessageEnabled — not just a binding path that happens to
/// compile. Same pattern/lesson as GroupPanelUiTests' settings-flyout coverage.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class ChatPanelUiTests
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

    [AvaloniaFact]
    public async Task ChatSettingsFlyout_TogglingCheckbox_UpdatesTheViewModel()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_ChatPanelUiTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(settingsService: new AppSettingsService(directory));

        try
        {
            var tool = new PanelTool
            {
                Id = "Chat",
                Title = "💬 Czat",
                ViewType = typeof(ChatPanelView),
                Context = viewModel,
            };
            var host = new PanelToolView { DataContext = tool };
            var window = new Window { Width = 500, Height = 400, Content = host };

            window.Show();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();

            // The generic placeholder must be hidden and the real Chat settings button visible —
            // same pair of checks the earlier Group "still shows the placeholder" bug needed.
            var placeholder = host.FindControl<Button>("SettingsButton")!;
            Assert.False(placeholder.IsVisible);
            var settingsButton = host.FindControl<Button>("ChatSettingsButton")!;
            Assert.True(settingsButton.IsVisible);

            settingsButton.Flyout!.ShowAt(settingsButton);
            Dispatcher.UIThread.RunJobs();

            var checkbox = window.GetVisualDescendants().OfType<CheckBox>()
                .Single(c => Equals(c.Content, "Dźwięk przy nowej wiadomości"));
            Assert.False(viewModel.ChatSoundOnNewMessageEnabled);

            checkbox.IsChecked = true;
            Pump(window);

            Assert.True(viewModel.ChatSoundOnNewMessageEnabled);

            window.Close();
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public void ChatOverlayCard_SettingsButton_IsVisibleInsteadOfThePlaceholder()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_ChatPanelUiTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(settingsService: new AppSettingsService(directory));

        try
        {
            var window = new MainWindow { DataContext = viewModel, Width = 1200, Height = 800 };
            window.Show();
            Pump(window);

            viewModel.ApplyLayoutCommand.Execute("TRANSPARENCY");
            Pump(window);

            var factory = Assert.IsType<MudDockFactory>(viewModel.Layout!.Factory);
            factory.AllTools.First(t => t.Id == "Chat").PinAsOverlayCommand.Execute(null);
            Pump(window);

            var card = window.GetVisualDescendants().OfType<TerminalOverlayCard>()
                .Single(c => c.Overlay?.Panel.Id == "Chat");

            // TerminalOverlayCard hosts its own copy of every settings button in its title bar
            // *and* embeds a PanelToolView underneath carrying a second, hidden copy — so there
            // can be more than one Button with a given tooltip; the real assertion is "exactly
            // one of them is visible", not "there is exactly one" (see GroupPanelUiTests).
            var placeholders = card.GetVisualDescendants().OfType<Button>()
                .Where(button => ToolTip.GetTip(button) as string == "Ustawienia panelu (wkrótce)")
                .ToList();
            Assert.NotEmpty(placeholders);
            Assert.All(placeholders, button => Assert.False(button.IsVisible));

            var realButtons = card.GetVisualDescendants().OfType<Button>()
                .Where(button => ToolTip.GetTip(button) as string == "Ustawienia czatu")
                .ToList();
            Assert.Single(realButtons, button => button.IsVisible);

            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
