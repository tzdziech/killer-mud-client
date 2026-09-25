using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MudClient.App.Models;
using MudClient.Core.BuffTimers;

namespace MudClient.App.Services;

/// <summary>Reads and writes history separately for every server character identity.</summary>
public sealed class SkillProgressHistoryStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };
    private readonly string _directory;

    public SkillProgressHistoryStore(string? settingsDirectory = null)
    {
        var root = settingsDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KillerMudClient");
        _directory = Path.Combine(root, "SkillProgress");
    }

    public SkillProgressHistoryDocument Load(BuffCharacterKey character)
    {
        var path = GetPath(character);
        if (!DurableJsonFile.TryRead<SkillProgressHistoryDocument>(path, SerializerOptions, out var document)
            || document is null)
        {
            return new SkillProgressHistoryDocument { Character = character };
        }

        document.Character = character;
        document.Events ??= [];
        return document;
    }

    public void Save(SkillProgressHistoryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document.Character);
        document.Events ??= [];
        DurableJsonFile.Write(GetPath(document.Character), document, SerializerOptions);
    }

    internal string GetPath(BuffCharacterKey character)
    {
        var readable = Sanitize($"{character.Host}-{character.Port}-{character.CharacterName}");
        var identity = $"{character.Host}\n{character.Port}\n{character.CharacterName}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity)))[..12].ToLowerInvariant();
        return Path.Combine(_directory, $"{readable}-{hash}.json");
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string(value.Select(character => invalid.Contains(character) ? '_' : character).ToArray());
    }
}
