using System.Reflection;
using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

internal static class SpellMobCatalogLoader
{
    private const string ResourceName = "MudClient.App.Assets.Data.spell-mobs.json";
    private static readonly Lazy<IReadOnlyList<SpellMobEntry>> Catalog = new(LoadCore);

    public static IReadOnlyList<SpellMobEntry> Load() => Catalog.Value;

    private static IReadOnlyList<SpellMobEntry> LoadCore()
    {
        using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Brak osadzonej bazy mobów z księgami: {ResourceName}.");

        var source = JsonSerializer.Deserialize<SpellMobDto[]>(
            resource,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Osadzona baza mobów z księgami jest pusta.");

        return source.Select(dto => new SpellMobEntry(
            RoomVnum: NormalizeRoomVnum(dto.RoomVnum),
            Mob: dto.Mob,
            Region: dto.Region,
            Class: dto.Class,
            Spells: dto.Spells,
            Notes: dto.Notes,
            Roaming: IsTrue(dto.Roaming),
            Dangerous: IsTrue(dto.Dangerous),
            Locked: IsTrue(dto.Locked),
            Boss: IsTrue(dto.Boss),
            Difficulty: dto.Difficulty)).ToArray();
    }

    private static bool IsTrue(string? flag) => flag == "t";

    // A roomVnum of 0 marks a roaming mob with no fixed room (see the "roaming" flag) — treated
    // the same as "no known location" so it doesn't collapse onto room 0 on the map.
    private static string? NormalizeRoomVnum(int roomVnum) => roomVnum <= 0 ? null : roomVnum.ToString();

    private sealed class SpellMobDto
    {
        public int RoomVnum { get; init; }
        public string Mob { get; init; } = string.Empty;
        public string Region { get; init; } = string.Empty;
        public string Class { get; init; } = string.Empty;
        public string[] Spells { get; init; } = [];
        public string? Notes { get; init; }
        public string? Roaming { get; init; }
        public string? Dangerous { get; init; }
        public string? Locked { get; init; }
        public string? Boss { get; init; }
        public int? Difficulty { get; init; }
    }
}
