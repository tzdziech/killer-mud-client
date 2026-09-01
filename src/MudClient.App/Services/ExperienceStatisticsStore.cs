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

    public ExperienceStatisticsData Load(string profileName)
    {
        var path = GetPath(profileName);
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

    public void Save(string profileName, ExperienceStatisticsData data)
    {
        Directory.CreateDirectory(DirectoryPath);
        var path = GetPath(profileName);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private string GetPath(string profileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = string.Concat(profileName.Select(character => invalid.Contains(character) ? '_' : character));
        return Path.Combine(DirectoryPath, safe + ".json");
    }
}
