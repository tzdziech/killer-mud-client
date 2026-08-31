using MudClient.Core.Text;

namespace MudClient.Core.Map;

/// <summary>
/// Shared decisions for one movement step reported by GMCP. Both autowalk and
/// interactive map movement use this policy so named exits and doors cannot
/// drift into subtly different command formats.
/// </summary>
public static class RoomMovementPolicy
{
    public static RoomExitInfo? FindExit(
        string directionOrName,
        IReadOnlyList<RoomExitInfo> exits)
    {
        var canonical = CanonicalDirection(directionOrName);

        return exits.FirstOrDefault(exit =>
            string.Equals(CanonicalDirection(exit.Dir), canonical, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exit.Name, directionOrName, StringComparison.OrdinalIgnoreCase));
    }

    public static string GetMoveCommand(RoomExitInfo exit) =>
        string.IsNullOrWhiteSpace(exit.Name)
            ? exit.Dir
            : PolishText.Fold(exit.Name);

    public static string? GetOpenCommand(RoomExitInfo? exit)
    {
        if (exit is null || !exit.HasDoor || !exit.IsClosed)
        {
            return null;
        }

        var target = string.IsNullOrWhiteSpace(exit.Name)
            ? exit.Dir
            : PolishText.Fold(exit.Name);
        return $"open {target}";
    }

    public static IReadOnlyList<string> BuildInitialCommands(RoomExitInfo exit)
    {
        var moveCommand = GetMoveCommand(exit);
        return GetOpenCommand(exit) is { } openCommand
            ? [openCommand, moveCommand]
            : [moveCommand];
    }

    /// <summary>Maps full map direction names and GMCP abbreviations to one representation.</summary>
    public static string CanonicalDirection(string direction) => direction.Trim().ToLowerInvariant() switch
    {
        "north" or "n" => "N",
        "south" or "s" => "S",
        "east" or "e" => "E",
        "west" or "w" => "W",
        "northeast" or "ne" => "NE",
        "northwest" or "nw" => "NW",
        "southeast" or "se" => "SE",
        "southwest" or "sw" => "SW",
        "up" or "u" => "U",
        "down" or "d" => "D",
        var value => value.ToUpperInvariant(),
    };
}
