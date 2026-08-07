using MudClient.Core.Text;

namespace MudClient.Core.Automation;

/// <summary>
/// Pure combat auto-response decisions: recognizing a knockdown or a disarm, either from the
/// live GMCP position or from the MUD's own text, independent of the "Walka" toggles that decide
/// whether to actually act on them.
/// </summary>
public static class CombatStatusPolicy
{
    /// <summary>True when the GMCP position reports the character lying down (knocked down).</summary>
    public static bool IsLyingPosition(string? position) =>
        string.Equals(position, "lying", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the line reports the character being knocked to the ground in combat.</summary>
    public static bool IsKnockedDownLine(string line) =>
        PolishText.Fold(line).Contains("powala cie na ziemie", StringComparison.OrdinalIgnoreCase);

    /// <summary>True when the line reports the character being disarmed in combat.</summary>
    public static bool IsDisarmedLine(string line) =>
        PolishText.Fold(line).Contains("rozbraja cie", StringComparison.OrdinalIgnoreCase);
}
