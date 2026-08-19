using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>Persists the user-defined "cast this spell on this group member" shortcuts shown as
/// buttons in the Group panel (see <see cref="Models.GroupSpellShortcut"/>). Like
/// <see cref="AbilityCaptureStore"/>/<see cref="ArtifactTryStore"/>, there's no embedded baseline
/// — a missing file just means the user hasn't defined any shortcuts yet.</summary>
public sealed class GroupSpellStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly string _path;

    public GroupSpellStore(string? path = null)
    {
        _path = path ?? System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KillerMudClient",
            "group-spells.json");
    }

    public string Path => _path;

    public GroupSpellDocument Load()
    {
        if (!File.Exists(_path))
        {
            return new GroupSpellDocument();
        }

        try
        {
            using var file = File.OpenRead(_path);
            return JsonSerializer.Deserialize<GroupSpellDocument>(file, SerializerOptions)
                ?? new GroupSpellDocument();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Baza skrótów czarów drużyny ma nieprawidłowy format JSON.", exception);
        }
    }

    public async Task SaveAsync(GroupSpellDocument document, CancellationToken cancellationToken = default)
    {
        var directory = System.IO.Path.GetDirectoryName(_path)
            ?? throw new InvalidOperationException("Ścieżka bazy skrótów czarów drużyny nie ma katalogu nadrzędnego.");
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
                    document,
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
