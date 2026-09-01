namespace MudClient.App.Models;

/// <summary>
/// Static, user-facing help for one dockable panel. It deliberately describes existing UI
/// semantics only; live state and product rules remain owned by the panel view models.
/// </summary>
public sealed record PanelHelpTopic(
    string PanelId,
    string Title,
    string Overview,
    IReadOnlyList<string> Indicators,
    string Settings,
    IReadOnlyList<string> Shortcuts)
{
    public bool HasIndicators => Indicators.Count > 0;

    public bool HasShortcuts => Shortcuts.Count > 0;
}
