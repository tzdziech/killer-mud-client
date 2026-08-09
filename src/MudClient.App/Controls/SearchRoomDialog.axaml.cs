using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MudClient.App.Controls;

internal sealed partial class SearchRoomDialog : Window
{
    public SearchRoomDialog()
    {
        InitializeComponent();
        Opened += (_, _) => VnumTextBox.Focus();
    }

    internal static Task<string?> ShowAsync(Window owner) =>
        new SearchRoomDialog().ShowDialog<string?>(owner);

    private void Cancel_OnClick(object? sender, RoutedEventArgs eventArgs) => Close(null);

    private void Search_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        var vnum = VnumTextBox.Text?.Trim();
        Close(string.IsNullOrEmpty(vnum) ? null : vnum);
    }
}
