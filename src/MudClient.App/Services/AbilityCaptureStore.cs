using System.Reflection;
using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>Persists the "help &lt;name&gt;" text captured live by "/mapuj &lt;class&gt;" (see
/// <see cref="AbilityMappingCoordinator"/>). Like <see cref="BookCatalogStore"/>/
/// <see cref="RareCatalogStore"/>, a missing local file falls back to a baseline embedded in the
/// app itself — everyone gets whatever has already been captured and shipped, and "/mapuj" on top
/// of that only adds what's still missing rather than starting from nothing.</summary>
public sealed class AbilityCaptureStore
{
    private const string EmbeddedResourceName = "MudClient.App.Assets.Data.ability-help.json";
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
        try
        {
            if (File.Exists(_path))
            {
                using var file = File.OpenRead(_path);
                return Deserialize(file);
            }

            using var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException($"Brak osadzonej bazy umiejętności/zaklęć: {EmbeddedResourceName}.");
            return Deserialize(embedded);
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

    private static AbilityCaptureDocument Deserialize(Stream stream) =>
        JsonSerializer.Deserialize<AbilityCaptureDocument>(stream, SerializerOptions)
        ?? throw new InvalidDataException("Baza umiejętności/zaklęć jest pusta.");
}
