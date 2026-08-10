using System.Net.Http.Json;
using System.Reflection;

namespace MudClient.App.Services;

public interface IAppUpdateService
{
    Task<AppUpdateAvailability?> CheckForUpdateAsync(CancellationToken cancellationToken = default);
}

/// <summary>A newer app release than the one currently running, per the published version
/// manifest — <see cref="DownloadPageUri"/> is always the fork's download page, which resolves
/// its own "Pobierz" button to the matching GitHub release tag client-side.</summary>
public sealed record AppUpdateAvailability(string Version, bool Prerelease, Uri DownloadPageUri);

/// <summary>
/// Checks the fork's GitHub Pages site for a newer app release than the one currently running.
/// Mirrors <see cref="ContentUpdateService"/>'s manifest-fetch shape, but for the app itself —
/// there's no in-client install step (replacing a running .exe needs a separate updater process),
/// so this only ever reports availability; the user downloads and runs the new build themselves.
/// </summary>
internal sealed class AppUpdateService : IAppUpdateService
{
    internal static readonly Uri DefaultManifestUri = new(
        "https://grzyboll.github.io/killer-mud-client/app-version.json");

    private static readonly Uri DownloadPageUri = new(
        "https://grzyboll.github.io/killer-mud-client/index.html");

    private static readonly HttpClient SharedHttpClient = CreateHttpClient();

    private readonly HttpClient _httpClient;
    private readonly Uri _manifestUri;
    private readonly Version _appVersion;

    public AppUpdateService(HttpClient? httpClient = null, Uri? manifestUri = null, string? appVersion = null)
    {
        _httpClient = httpClient ?? SharedHttpClient;
        _manifestUri = manifestUri ?? DefaultManifestUri;
        _appVersion = ParseVersion(appVersion ?? GetCurrentVersion()) ?? new Version(0, 0);
    }

    public async Task<AppUpdateAvailability?> CheckForUpdateAsync(
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));

        var manifest = await _httpClient.GetFromJsonAsync<AppVersionManifest>(_manifestUri, timeout.Token)
            .ConfigureAwait(false);
        return EvaluateManifest(manifest, _appVersion);
    }

    internal static AppUpdateAvailability? EvaluateManifest(AppVersionManifest? manifest, Version currentVersion)
    {
        if (manifest is null || manifest.SchemaVersion != 1 || string.IsNullOrWhiteSpace(manifest.Version))
        {
            return null;
        }

        var latestVersion = ParseVersion(manifest.Version);
        if (latestVersion is null || latestVersion <= currentVersion)
        {
            return null;
        }

        return new AppUpdateAvailability(manifest.Version, manifest.Prerelease, DownloadPageUri);
    }

    internal static string GetCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(AppUpdateService).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
               ?? assembly.GetName().Version?.ToString()
               ?? "0.0.0";
    }

    private static Version? ParseVersion(string? value)
    {
        var core = value?.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        return Version.TryParse(core, out var version) ? version : null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("KillerMudClient-AppUpdateChecker");
        return client;
    }

    internal sealed class AppVersionManifest
    {
        public int SchemaVersion { get; init; }
        public string Version { get; init; } = string.Empty;
        public bool Prerelease { get; init; }
    }
}
