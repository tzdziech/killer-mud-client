using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Services;
using MudClient.App.ViewModels;

namespace MudClient.App.Tests;

/// <summary>Covers the "Pokaż liczby przy score" setting — splices " (N-M)" onto the end of
/// recognized "score" stat lines, in place, as they arrive. Same OnTextReceived/dispatcher-pump
/// approach as NumericDamageTests.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class ScoreAnnotationTests
{
    private static void InvokeOnTextReceived(MainWindowViewModel viewModel, string text)
    {
        var method = typeof(MainWindowViewModel).GetMethod(
            "OnTextReceived", BindingFlags.NonPublic | BindingFlags.Instance)!;
        method.Invoke(viewModel, [text]);
        Dispatcher.UIThread.RunJobs();
    }

    private static (MainWindowViewModel ViewModel, List<string> Output, string Directory) CreateViewModel()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_ScoreAnnotationTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(settingsService: new AppSettingsService(directory));
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);
        return (viewModel, output, directory);
    }

    [AvaloniaFact]
    public async Task ScoreLine_WithSettingEnabled_AppendsRangeToTheSameLine()
    {
        var (viewModel, output, directory) = CreateViewModel();

        try
        {
            Assert.True(viewModel.AnnotateScoreEnabled);
            InvokeOnTextReceived(viewModel, "Twoja sila jest srednia.\n");

            Assert.Contains(output, text => text.Contains("Twoja sila jest srednia. (116-129)"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task ScoreLine_SplitAcrossTwoChunks_IsStillAnnotated()
    {
        var (viewModel, output, directory) = CreateViewModel();

        try
        {
            InvokeOnTextReceived(viewModel, "Twoja zrecz");
            InvokeOnTextReceived(viewModel, "nosc jest niezla.\n");

            Assert.Contains(output, text => text.Contains("nosc jest niezla. (144-157)"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task ScoreLine_WithSettingDisabled_IsNotAnnotated()
    {
        var (viewModel, output, directory) = CreateViewModel();

        try
        {
            viewModel.AnnotateScoreEnabled = false;
            InvokeOnTextReceived(viewModel, "Twoja sila jest srednia.\n");

            Assert.Contains(output, text => text.Contains("Twoja sila jest srednia.\n"));
            Assert.DoesNotContain(output, text => text.Contains("(116-129)"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task NonScoreLine_IsNeverAnnotated()
    {
        var (viewModel, output, directory) = CreateViewModel();

        try
        {
            InvokeOnTextReceived(viewModel, "Rozglądasz się dookoła.\n");

            Assert.DoesNotContain(output, text => text.Contains('('));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task IncompleteScoreLine_WithoutNewline_IsForwardedUnmodified()
    {
        var (viewModel, output, directory) = CreateViewModel();

        try
        {
            InvokeOnTextReceived(viewModel, "Twoja sila jest srednia.");

            Assert.Contains(output, text => text == "Twoja sila jest srednia.");
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
