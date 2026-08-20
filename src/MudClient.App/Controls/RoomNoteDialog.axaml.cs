using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MudClient.App.Controls;

internal sealed partial class RoomNoteDialog : Window
{
    public RoomNoteDialog(string? initialNote)
    {
        InitializeComponent();
        NoteTextBox.Text = initialNote;
        Opened += (_, _) =>
        {
            NoteTextBox.Focus();
            NoteTextBox.SelectAll();
        };
    }

    /// <summary>Null means cancelled (no change); an empty string means "save with no text",
    /// which MapViewModel.SetNoteOnSelectedRoom treats as clearing any existing note.</summary>
    internal static Task<string?> ShowAsync(Window owner, string? initialNote) =>
        new RoomNoteDialog(initialNote).ShowDialog<string?>(owner);

    private void Cancel_OnClick(object? sender, RoutedEventArgs eventArgs) => Close(null);

    private void Save_OnClick(object? sender, RoutedEventArgs eventArgs) => Close(NoteTextBox.Text ?? string.Empty);
}
