namespace MudClient.App.Models;

/// <summary>A user-defined free-form command shortcut shown as a button in the "Akcje offensywne
/// i definiowalne" panel's Definiowalne section — clicking it sends <see cref="Command"/>
/// verbatim, with <see cref="Label"/> as the button text.</summary>
public sealed class CustomCommandShortcut
{
    public string Label { get; set; } = string.Empty;

    public string Command { get; set; } = string.Empty;
}
