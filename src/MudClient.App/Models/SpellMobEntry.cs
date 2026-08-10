namespace MudClient.App.Models;

public sealed record SpellMobEntry(
    string? RoomVnum,
    string Mob,
    string Region,
    string Class,
    IReadOnlyList<string> Spells,
    string? Notes,
    bool Roaming,
    bool Dangerous,
    bool Locked,
    bool Boss,
    int? Difficulty)
{
    public bool HasRoomLocation => !string.IsNullOrWhiteSpace(RoomVnum);

    public string SpellsText => Spells.Count == 0 ? "brak danych" : string.Join(", ", Spells);
}
