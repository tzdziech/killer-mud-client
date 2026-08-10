using MudClient.App.Services;
using MudClient.App.ViewModels;

namespace MudClient.App.Tests;

public sealed class AppUpdateNotificationTests : IAsyncDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "KillerMudClient-AppUpdateNotificationTests", Guid.NewGuid().ToString("N"));
    private MainWindowViewModel? _viewModel;

    [Fact]
    public async Task StartAppUpdateCheck_NewerVersionAvailable_UpdatesStateAndToasts()
    {
        var appUpdateService = new RecordingAppUpdateService(
            new AppUpdateAvailability("9.9.9", Prerelease: true, new Uri("https://example.test/download")));
        _viewModel = new MainWindowViewModel(
            settingsService: new AppSettingsService(_directory),
            appUpdateService: appUpdateService);

        _viewModel.StartAppUpdateCheck();
        Assert.NotNull(_viewModel.ActiveAppUpdateCheck);
        await _viewModel.ActiveAppUpdateCheck;

        Assert.True(_viewModel.IsAppUpdateAvailable);
        Assert.Contains("9.9.9", _viewModel.AppUpdateStatus);
        Assert.True(_viewModel.OpenAppUpdateCommand.CanExecute(null));
        Assert.Contains(_viewModel.Toasts, t => t.Type == "info" && t.Text.Contains("9.9.9"));
    }

    [Fact]
    public async Task StartAppUpdateCheck_NoNewerVersion_LeavesUpdateUnavailableAndDoesNotToast()
    {
        var appUpdateService = new RecordingAppUpdateService(update: null);
        _viewModel = new MainWindowViewModel(
            settingsService: new AppSettingsService(_directory),
            appUpdateService: appUpdateService);

        _viewModel.StartAppUpdateCheck();
        await _viewModel.ActiveAppUpdateCheck!;

        Assert.False(_viewModel.IsAppUpdateAvailable);
        Assert.False(_viewModel.OpenAppUpdateCommand.CanExecute(null));
        Assert.DoesNotContain(_viewModel.Toasts, t => t.Text.Contains("nowa wersja klienta"));
    }

    public async ValueTask DisposeAsync()
    {
        if (_viewModel is not null)
        {
            await _viewModel.DisposeAsync();
        }

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private sealed class RecordingAppUpdateService(AppUpdateAvailability? update) : IAppUpdateService
    {
        public Task<AppUpdateAvailability?> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(update);
    }
}
