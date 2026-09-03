using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

public sealed class ExperienceStatisticsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public ExperienceStatisticsStore(string? directoryPath = null)
    {
        DirectoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "KillerMudClient", "Statistics");
    }

    public string DirectoryPath { get; }

    public ExperienceStatisticsData Load(string characterName)
    {
        var path = GetPath(characterName);
        if (!File.Exists(path)) return new ExperienceStatisticsData();
        try
        {
            return JsonSerializer.Deserialize<ExperienceStatisticsData>(File.ReadAllText(path), JsonOptions)
                ?? new ExperienceStatisticsData();
        }
        catch (JsonException)
        {
            // A damaged optional statistics file must not prevent the character from connecting.
            return new ExperienceStatisticsData();
        }
    }

    public void Save(string characterName, ExperienceStatisticsData data)
    {
        var path = GetPath(characterName);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private string GetPath(string characterName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(characterName.Select(character => invalid.Contains(character) ? '_' : character));
        // Legacy files in the parent directory are account-scoped and may mix characters.
        // Never automatically import them, even if an account happens to share this name.
        return Path.Combine(DirectoryPath, "Characters", safe + ".json");
    }
}
