using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views.Panels;

namespace MudClient.App.Tests;

/// <summary>Covers the visual wiring for the "/" command autocomplete popup (see
/// MainWindowViewModel.CommandSuggestions/HasCommandSuggestions) — that the Popup and its ListBox
/// actually reflect the ViewModel state through their bindings. The suggestion-generation and
/// keyboard-navigation logic itself (filtering, clamping, accept/clear) is covered at the
/// ViewModel level in MainWindowViewModelTests, since it needs no view at all.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class TerminalCommandAutocompleteTests
{
    private static (MainWindowViewModel ViewModel, string Directory) CreateViewModel()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_TerminalAutocompleteTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return (new MainWindowViewModel(settingsService: new AppSettingsService(directory)), directory);
    }

    [AvaloniaFact]
    public async Task Popup_OpensAndListsMatches_WhenTypingASlashCommand()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var terminal = new TerminalPanelView { DataContext = viewModel };
            var window = new Window { Width = 800, Height = 500, Content = terminal };
            window.Show();

            var popup = terminal.FindControl<Popup>("CommandSuggestionsPopup")!;
            Assert.False(popup.IsOpen);

            viewModel.CommandText = "/autoas";
            Dispatcher.UIThread.RunJobs();

            Assert.True(popup.IsOpen);
            var list = terminal.FindControl<ListBox>("CommandSuggestionsList")!;
            Assert.Equal(viewModel.CommandSuggestions.Count, list.ItemCount);
            Assert.Contains("autoassist", viewModel.CommandSuggestions);

            window.Close();
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task Popup_Closes_OnceTheCommandNameIsFullyTypedAndSpaced()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var terminal = new TerminalPanelView { DataContext = viewModel };
            var window = new Window { Width = 800, Height = 500, Content = terminal };
            window.Show();

            viewModel.CommandText = "/autostand";
            Dispatcher.UIThread.RunJobs();
            var popup = terminal.FindControl<Popup>("CommandSuggestionsPopup")!;
            Assert.True(popup.IsOpen);

            viewModel.CommandText = "/autostand on";
            Dispatcher.UIThread.RunJobs();

            Assert.False(popup.IsOpen);

            window.Close();
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
