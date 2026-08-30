using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using MudClient.App.Models;
using MudClient.App.ViewModels;

namespace MudClient.App.Views.Panels;

public sealed partial class OffensiveActionsPanelView : UserControl
{
    private MainWindowViewModel? _subscribedViewModel;

    public OffensiveActionsPanelView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Rebuild();
    }

    protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Rebuild();
    }

    /// <summary>Lays SectionsGrid out stacked (default: Offensywne over Definiowalne, separated by
    /// a horizontal line) or side by side (Offensywne left, Definiowalne right, separated by a
    /// vertical line) depending on MainWindowViewModel.OffensiveSectionsSideBySide. Grid.Row/
    /// Grid.Column/Grid.RowDefinitions/Grid.ColumnDefinitions aren't simple bindable values in
    /// Avalonia, so this stays in code-behind rather than a converter — mirrors PanelToolView's
    /// own Rebuild() pattern.</summary>
    private void Rebuild()
    {
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _subscribedViewModel = DataContext as MainWindowViewModel;
        if (_subscribedViewModel is not null)
        {
            _subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        ApplyLayout();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.PropertyName is nameof(MainWindowViewModel.OffensiveSectionsSideBySide))
        {
            ApplyLayout();
        }
    }

    private void ApplyLayout()
    {
        var grid = this.FindControl<Grid>("SectionsGrid")!;
        var offensiveSection = this.FindControl<StackPanel>("OffensiveSection")!;
        var separator = this.FindControl<Border>("SectionsSeparator")!;
        var definableSection = this.FindControl<StackPanel>("DefinableSection")!;
        var sideBySide = _subscribedViewModel?.OffensiveSectionsSideBySide == true;

        if (sideBySide)
        {
            grid.RowDefinitions = new RowDefinitions("*");
            grid.ColumnDefinitions = new ColumnDefinitions("*,Auto,*");
            Grid.SetRow(offensiveSection, 0);
            Grid.SetColumn(offensiveSection, 0);
            Grid.SetRow(separator, 0);
            Grid.SetColumn(separator, 1);
            Grid.SetRow(definableSection, 0);
            Grid.SetColumn(definableSection, 2);
            separator.Width = 1;
            separator.Height = double.NaN;
            separator.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;
            separator.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center;
            separator.Margin = new Avalonia.Thickness(10, 0);
        }
        else
        {
            grid.RowDefinitions = new RowDefinitions("Auto,Auto,Auto");
            grid.ColumnDefinitions = new ColumnDefinitions("*");
            Grid.SetRow(offensiveSection, 0);
            Grid.SetColumn(offensiveSection, 0);
            Grid.SetRow(separator, 1);
            Grid.SetColumn(separator, 0);
            Grid.SetRow(definableSection, 2);
            Grid.SetColumn(definableSection, 0);
            separator.Height = 1;
            separator.Width = double.NaN;
            separator.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            separator.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            separator.Margin = new Avalonia.Thickness(0, 10);
        }
    }

    private void OnCastOffensiveActionClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { DataContext: OffensiveActionShortcut shortcut })
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.CastOffensiveAction(shortcut);
        }
    }

    private void OnSendCustomCommandClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { DataContext: CustomCommandShortcut shortcut })
        {
            return;
        }

        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SendCustomCommand(shortcut);
        }
    }
}
