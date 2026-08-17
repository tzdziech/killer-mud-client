using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MudClient.App.Controls;

/// <summary>
/// Reusable, fixed-layout overlay for large widget screens. The frame occupies 90% of its
/// host in both dimensions by default, or nearly the full host when <see cref="IsFullSize"/>
/// is set; callers provide the tab control through <see cref="TabContent"/> and toggle
/// <see cref="IsOpen"/>.
/// </summary>
public partial class LargeTabbedWidget : UserControl
{
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<LargeTabbedWidget, bool>(
            nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<LargeTabbedWidget, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<bool> IsBookStyleProperty =
        AvaloniaProperty.Register<LargeTabbedWidget, bool>(nameof(IsBookStyle));

    /// <summary>True shrinks the frame's edge margin from 5% to 1% per side, so the widget
    /// fills almost the whole host window instead of 90% of it — for a screen (like
    /// Killeropedia's skill tree) that genuinely needs the room, rather than every large
    /// widget by default. Bound per-column/row in the XAML (see <see cref="EdgeWidthConverter"/>
    /// and <see cref="MiddleWidthConverter"/>) rather than swapped in from code-behind: replacing
    /// a Grid's whole <c>ColumnDefinitions</c>/<c>RowDefinitions</c> collection at runtime doesn't
    /// reliably invalidate its layout in Avalonia, whereas binding each column's <c>Width</c>
    /// does.</summary>
    public static readonly StyledProperty<bool> IsFullSizeProperty =
        AvaloniaProperty.Register<LargeTabbedWidget, bool>(nameof(IsFullSize));

    public static readonly StyledProperty<TabControl?> TabContentProperty =
        AvaloniaProperty.Register<LargeTabbedWidget, TabControl?>(nameof(TabContent));

    /// <summary>Maps <see cref="IsFullSize"/> to the star-width of the outer grid's edge
    /// (margin) columns/rows: 1 star when full-size, 5 otherwise.</summary>
    public static readonly IValueConverter EdgeWidthConverter =
        new FuncValueConverter<bool, GridLength>(isFullSize => new GridLength(isFullSize ? 1 : 5, GridUnitType.Star));

    /// <summary>Maps <see cref="IsFullSize"/> to the star-width of the outer grid's middle
    /// (content) column/row: 98 stars when full-size, 90 otherwise.</summary>
    public static readonly IValueConverter MiddleWidthConverter =
        new FuncValueConverter<bool, GridLength>(isFullSize => new GridLength(isFullSize ? 98 : 90, GridUnitType.Star));

    public LargeTabbedWidget()
    {
        InitializeComponent();
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public bool IsBookStyle
    {
        get => GetValue(IsBookStyleProperty);
        set => SetValue(IsBookStyleProperty, value);
    }

    public bool IsFullSize
    {
        get => GetValue(IsFullSizeProperty);
        set => SetValue(IsFullSizeProperty, value);
    }

    public TabControl? TabContent
    {
        get => GetValue(TabContentProperty);
        set => SetValue(TabContentProperty, value);
    }

    private void Close_OnClick(object? sender, RoutedEventArgs eventArgs) => Close();

    private void OnKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Escape)
        {
            return;
        }

        Close();
        eventArgs.Handled = true;
    }

    private void Close()
    {
        IsOpen = false;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
