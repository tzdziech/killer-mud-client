using Avalonia.Controls;
using Avalonia.Interactivity;
using MudClient.App.Models;
using MudClient.App.ViewModels;

namespace MudClient.App.Views.Panels;

public sealed partial class GroupPanelView : UserControl
{
    public GroupPanelView()
    {
        InitializeComponent();
    }

    private void GroupContextMenu_OnOpened(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetGroupContextMenuOpen(true);
        }
    }

    private void GroupContextMenu_OnClosed(object? sender, RoutedEventArgs eventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetGroupContextMenuOpen(false);
        }
    }

    private void OnCastGroupSpellClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { DataContext: GroupSpellShortcut shortcut, Tag: GroupMember member })
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CastGroupSpellOnMember(member, shortcut);
        }
    }
}
