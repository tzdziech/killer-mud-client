using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MudClient.App.Controls;

internal sealed partial class SearchTeacherDialog : Window
{
    public SearchTeacherDialog()
    {
        InitializeComponent();
        Opened += (_, _) => NameTextBox.Focus();
    }

    internal static Task<string?> ShowAsync(Window owner) =>
        new SearchTeacherDialog().ShowDialog<string?>(owner);

    private void Cancel_OnClick(object? sender, RoutedEventArgs eventArgs) => Close(null);

    private void Search_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        var name = NameTextBox.Text?.Trim();
        Close(string.IsNullOrEmpty(name) ? null : name);
    }
}
