namespace MudClient.Core.Automation;

/// <summary>One entry in auto-farm's "cast on combat start" sequence (see
/// <see cref="AutoFarmCastSequencePolicy"/>). A buff (<c>Offensive: false</c>) is cast on self and
/// skipped once already an active buff; an offensive spell (<c>Offensive: true</c>) is aimed at
/// whichever mob the character is currently fighting instead, and always fires — there's no
/// "already active" state to check for a damage spell. Mixing the two under a single "self"
/// target is what previously got an offensive entry rejected by the MUD ("Nie da rady tego
/// zrobic").</summary>
public sealed record AutoFarmCastSpell(string Name, bool Offensive);
