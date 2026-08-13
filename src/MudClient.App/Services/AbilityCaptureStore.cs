using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>Persists the "help &lt;name&gt;" text captured live by "/mapuj &lt;class&gt;" (see
/// <see cref="AbilityMappingCoordinator"/>). Unlike <see cref="BookCatalogStore"/>/
/// <see cref="RareCatalogStore"/>, there's no embedded baseline to fall back to — this catalog only
/// exists once a user has actually run "/mapuj" at least once, so a missing file just means an
/// empty document.</summary>
public sealed class AbilityCaptureStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public AbilityCaptureStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KillerMudClient",
            "ability-help.json");
    }

    public string Path => _path;

    public AbilityCaptureDocument Load()
    {
        if (!File.Exists(_path))
        {
            return new AbilityCaptureDocument();
        }

        try
        {
            using var file = File.OpenRead(_path);
            return JsonSerializer.Deserialize<AbilityCaptureDocument>(file, SerializerOptions)
                ?? new AbilityCaptureDocument();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Baza umiejętności/zaklęć ma nieprawidłowy format JSON.", exception);
        }
    }

    public async Task SaveAsync(AbilityCaptureDocument catalog, CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Ścieżka bazy umiejętności/zaklęć nie ma katalogu nadrzędnego.");
        Directory.CreateDirectory(directory);
        var temporaryPath = _path + ".tmp";

        try
        {
            await using (var stream = new FileStream(
                temporaryPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                16 * 1024,
                useAsync: true))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    catalog,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryPath, _path, overwrite: true);
        }
        finally
        {
            // A cancelled or failed run must not leave a partial file that could be loaded later.
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
