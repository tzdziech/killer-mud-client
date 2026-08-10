using Avalonia.Controls;
using Avalonia.Interactivity;
using MudClient.App.ViewModels;

namespace MudClient.App.Controls;

internal sealed partial class SearchTeacherDialog : Window
{
    public SearchTeacherDialog(IReadOnlyList<TeacherSearchEntry> entries)
    {
        InitializeComponent();
        NameBox.ItemsSource = entries;
        NameBox.ItemFilter = (search, item) =>
            item is TeacherSearchEntry entry
            && !string.IsNullOrEmpty(search)
            && entry.SearchText.Contains(search, StringComparison.OrdinalIgnoreCase);
        Opened += (_, _) => NameBox.Focus();
    }

    internal static Task<string?> ShowAsync(Window owner, IReadOnlyList<TeacherSearchEntry> entries) =>
        new SearchTeacherDialog(entries).ShowDialog<string?>(owner);

    private void Cancel_OnClick(object? sender, RoutedEventArgs eventArgs) => Close(null);

    private void Search_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        var name = (NameBox.SelectedItem as TeacherSearchEntry)?.Name ?? NameBox.Text?.Trim();
        Close(string.IsNullOrEmpty(name) ? null : name);
    }
}
