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

    /// <summary>Every known way the MUD reports the character ending up on the ground —
    /// knocked down in combat, tripping, or slumping half-conscious.</summary>
    private static readonly string[] KnockedDownPhrases =
    [
        "powala cie na ziemie",
        "przewracasz sie",
        "osuwasz sie polprzytomny",
    ];

    /// <summary>True when the line reports the character ending up on the ground (knocked down,
    /// tripping, or slumping half-conscious) — see <see cref="KnockedDownPhrases"/>.</summary>
    public static bool IsKnockedDownLine(string line)
    {
        var folded = PolishText.Fold(line);
        return KnockedDownPhrases.Any(phrase => folded.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Every known way the MUD reports a weapon leaving the character's hands in
    /// combat.</summary>
    private static readonly string[] DisarmedPhrases =
    [
        "rozbraja cie",
        "wypada ci z rak",
    ];

    /// <summary>True when the line reports the character being disarmed in combat — see
    /// <see cref="DisarmedPhrases"/>.</summary>
    public static bool IsDisarmedLine(string line)
    {
        var folded = PolishText.Fold(line);
        return DisarmedPhrases.Any(phrase => folded.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }
}
