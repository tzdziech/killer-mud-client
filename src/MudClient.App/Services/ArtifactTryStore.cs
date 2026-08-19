using System.Reflection;
using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>Persists the "try &lt;n&gt;" text captured live by "/mapuj &lt;liczba&gt;" (see
/// <see cref="ArtifactTryMappingCoordinator"/>). Like <see cref="BookCatalogStore"/>/
/// <see cref="RareCatalogStore"/>, a missing local file falls back to a baseline embedded in the
/// app itself — everyone gets whatever has already been captured and shipped, and "/mapuj" on top
/// of that only adds what's still missing rather than starting from nothing.</summary>
public sealed class ArtifactTryStore
{
    private const string EmbeddedResourceName = "MudClient.App.Assets.Data.artifact-try.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public ArtifactTryStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KillerMudClient",
            "artifact-try.json");
    }

    public string Path => _path;

    public ArtifactTryDocument Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                using var file = File.OpenRead(_path);
                return Deserialize(file);
            }

            using var embedded = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)
                ?? throw new InvalidOperationException($"Brak osadzonej bazy artefaktów (try): {EmbeddedResourceName}.");
            return Deserialize(embedded);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Baza artefaktów (try) ma nieprawidłowy format JSON.", exception);
        }
    }

    public async Task SaveAsync(ArtifactTryDocument catalog, CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Ścieżka bazy artefaktów (try) nie ma katalogu nadrzędnego.");
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

    private static ArtifactTryDocument Deserialize(Stream stream) =>
        JsonSerializer.Deserialize<ArtifactTryDocument>(stream, SerializerOptions)
        ?? throw new InvalidDataException("Baza artefaktów (try) jest pusta.");
}
