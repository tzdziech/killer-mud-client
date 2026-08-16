namespace MudClient.App.Models;

/// <summary>
/// One boolean automation/preference toggle exposed both as a "/&lt;Command&gt; [on|off]" terminal
/// command (see <c>MainWindowViewModel.TryHandleSettingsToggleCommand</c>) and as a documented
/// entry in the Help panel's "Automatyzacje" tab (see <c>MainWindowViewModel.CommandToggles</c>) —
/// a single source of truth so the command and its documentation can't drift apart the way the
/// hand-copied Help text already had for other commands.
/// </summary>
public sealed record CommandToggleEntry(
    string Command,
    string DisplayName,
    string Description,
    Func<bool> Get,
    Action<bool> Set);
