using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using MudClient.App.Models;
using MudClient.App.ViewModels;
using System.Collections.Specialized;

namespace MudClient.App.Views.Panels;

public sealed partial class GroupPanelView : UserControl
{
    private readonly Dictionary<GroupMember, DispatcherTimer> _blinkTimers = new();
    private readonly Dictionary<GroupMember, bool> _blinkState = new(); // true = warning color, false = normal

    public GroupPanelView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Setup animations for existing group members
        ApplyAnimationsToAllBars();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel.Group is INotifyCollectionChanged notifyCollection)
            {
                notifyCollection.CollectionChanged -= OnGroupCollectionChanged;
                notifyCollection.CollectionChanged += OnGroupCollectionChanged;
            }
        }
    }

    private void OnGroupCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Cleanup old timers from removed items
        if (e.OldItems != null)
        {
            foreach (var oldItem in e.OldItems)
            {
                if (oldItem is GroupMember member && _blinkTimers.TryGetValue(member, out var timer))
                {
                    timer.Stop();
                    _blinkTimers.Remove(member);
                    _blinkState.Remove(member);
                }
            }
        }

        // Apply animations to new items
        ApplyAnimationsToAllBars();
    }

    private void ApplyAnimationsToAllBars()
    {
        if (GroupMembersList is not ItemsControl itemsControl)
            return;

        foreach (var container in itemsControl.GetRealizedContainers())
        {
            if (container.DataContext is GroupMember member && container is Control control)
            {
                // Find the HP ProgressBar in this container
                var hpBar = FindProgressBar(control, "HP");
                if (hpBar != null)
                {
                    StartOrUpdateHpBarAnimation(member, hpBar);
                }
            }
        }
    }

    private ProgressBar? FindProgressBar(Control control, string? hint = null)
    {
        // Navigate the visual tree to find ProgressBar with class "group-member-hp-bar"
        if (control is ProgressBar bar && bar.Classes.Contains("group-member-hp-bar"))
        {
            return bar;
        }

        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control childControl)
                {
                    var found = FindProgressBar(childControl, hint);
                    if (found != null)
                        return found;
                }
            }
        }

        return null;
    }

    private void StartOrUpdateHpBarAnimation(GroupMember member, ProgressBar bar)
    {
        // Stop existing timer if present
        if (_blinkTimers.TryGetValue(member, out var existingTimer))
        {
            existingTimer.Stop();
            _blinkTimers.Remove(member);
        }

        _blinkState[member] = false; // Start with normal color

        // Get blink interval based on HP%
        var blinkInterval = GetBlinkInterval(member.HpPercent);
        if (blinkInterval == null)
        {
            // No blinking needed - just set normal color
            var normalColor = Application.Current?.TryFindResource("MudColorCrimson", out var crimsonObj) == true
                ? (Color)crimsonObj
                : Color.Parse("#B04A4A");
            bar.Foreground = new SolidColorBrush(normalColor);
            return;
        }

        // Create and start timer
        var timer = new DispatcherTimer
        {
            Interval = blinkInterval.Value
        };

        timer.Tick += (s, e) =>
        {
            if (_blinkState.TryGetValue(member, out var isWarning))
            {
                var targetColor = isWarning
                    ? (Application.Current?.TryFindResource("MudColorCrimson", out var crimsonObj) == true
                        ? (Color)crimsonObj
                        : Color.Parse("#B04A4A"))
                    : (Application.Current?.TryFindResource("MudColorAmberWarn", out var amberObj) == true
                        ? (Color)amberObj
                        : Color.Parse("#D9922E"));

                bar.Foreground = new SolidColorBrush(targetColor);
                _blinkState[member] = !isWarning; // Toggle
            }
        };

        timer.Start();
        _blinkTimers[member] = timer;
    }

    private TimeSpan? GetBlinkInterval(double hpPercent)
    {
        // Return interval for one half-cycle (blink happens every interval)
        return hpPercent < 25.0
            ? TimeSpan.FromSeconds(0.3)   // Fast: 0.3s per half, 0.6s full cycle
            : hpPercent < 40.0
            ? TimeSpan.FromSeconds(0.6)   // Normal: 0.6s per half, 1.2s full cycle
            : hpPercent < 55.0
            ? TimeSpan.FromSeconds(1.0)   // Slow: 1.0s per half, 2s full cycle
            : null;                        // No blinking > 55%
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

