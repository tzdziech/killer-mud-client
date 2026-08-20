using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using MudClient.App.Controls;
using MudClient.App.Models;
using MudClient.App.ViewModels;

namespace MudClient.App.Views.Panels;

public sealed partial class MapPanelView : UserControl
{
    private MapViewModel? _viewModel;
    private bool _isViewModelSubscribed;

    /// <summary>Overridable in tests — see AutomationDeletionConfirmationUiTests.</summary>
    internal Func<Window, string, string, Task<bool>> ConfirmDeletionAsync { get; set; } =
        DeleteConfirmationDialog.ShowAsync;

    /// <summary>Overridable in tests — see MapSearchRoomUiTests.</summary>
    internal Func<Window, Task<string?>> SearchRoomAsync { get; set; } =
        SearchRoomDialog.ShowAsync;

    /// <summary>Overridable in tests — see MapSearchTeacherUiTests.</summary>
    internal Func<Window, IReadOnlyList<MapSearchEntry>, Task<string?>> SearchTeacherAsync { get; set; } =
        SearchTeacherDialog.ShowAsync;

    /// <summary>Overridable in tests — see RoomNoteUiTests.</summary>
    internal Func<Window, string?, Task<string?>> RoomNoteAsync { get; set; } =
        RoomNoteDialog.ShowAsync;

    public MapPanelView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        MapControl.RoomClicked += OnRoomClicked;
        MapControl.RoomDoubleClicked += OnRoomDoubleClicked;
        MapControl.ManualNavigationOccurred += OnManualNavigation;
        MapControl.MovementKeyPressed += OnMovementKeyPressed;
        MapControl.RegionSelected += OnRegionSelected;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        UnsubscribeFromViewModel();

        _viewModel = DataContext as MapViewModel;

        if (this.IsAttachedToVisualTree())
        {
            SubscribeToViewModel();
        }

        SyncControlFromViewModel();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SubscribeToViewModel();
        SyncControlFromViewModel();

        // Rendered inside a Terminal overlay card, MapMenuButton would duplicate the card's own
        // settings button in its title bar (see TerminalOverlayCard.axaml) — hide this one there.
        MapMenuButton.IsVisible = this.FindAncestorOfType<TerminalOverlayCard>() is null;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromViewModel();

        // The image cache publishes icon-load notifications. Leaving it assigned here would
        // retain every map control created by Dock while panels are closed and restored.
        MapControl.RoomImages = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void SubscribeToViewModel()
    {
        if (_viewModel is null || _isViewModelSubscribed)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.CenterOnCurrentRoomRequested += OnCenterRequested;
        _viewModel.CenterOnRoomRequested += OnCenterOnRoomRequested;
        _isViewModelSubscribed = true;
    }

    private void UnsubscribeFromViewModel()
    {
        if (_viewModel is null || !_isViewModelSubscribed)
        {
            return;
        }

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.CenterOnCurrentRoomRequested -= OnCenterRequested;
        _viewModel.CenterOnRoomRequested -= OnCenterOnRoomRequested;
        _isViewModelSubscribed = false;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        SyncControlFromViewModel();
    }

    private void SyncControlFromViewModel()
    {
        if (_viewModel is null)
        {
            return;
        }

        MapControl.MapIndex = _viewModel.MapIndex;
        MapControl.Settings = _viewModel.Settings;
        MapControl.TextureCache = _viewModel.TextureCache;
        MapControl.RoomImages = _viewModel.RoomImages;
        MapControl.AreaId = _viewModel.SelectedArea?.Id ?? 0;
        MapControl.Z = _viewModel.SelectedZ;
        MapControl.CurrentRoom = _viewModel.CurrentRoom;
        MapControl.SelectedRoom = _viewModel.SelectedRoom;
        MapControl.Route = _viewModel.RouteRooms;
        MapControl.GroupMarkers = _viewModel.GroupMarkers;
        MapControl.DeathMarkers = _viewModel.DeathMarkers;
        MapControl.RoomMarkers = _viewModel.RoomMarkers;
        MapControl.TeacherMarkers = _viewModel.TeacherMarkers;
        MapControl.SpellMobMarkers = _viewModel.SpellMobMarkers;
        MapControl.SpellKnowledge = _viewModel.SpellKnowledge;
        MapControl.SkillKnowledge = _viewModel.SkillKnowledge;
        MapControl.AutoFarmRegion = _viewModel.AutoFarmRegion;
        MapControl.AutoFarmVisitedRoomIds = _viewModel.AutoFarmVisitedRoomIds;
        MapControl.IsRegionSelectModeEnabled = _viewModel.IsDefiningAutoFarmRegion;
        MapControl.RoomsWithMissingSpell = _viewModel.RoomsWithMissingSpell;
        MapControl.ShowGroupMembersAsNumbers = _viewModel.ShowGroupMembersAsNumbers;
        MapControl.DisplayMode = _viewModel.SelectedDisplayMode.Mode;
    }

    private void OnRoomDoubleClicked(MudClient.Core.Map.MapRoom room)
    {
        _viewModel?.NotifyRoomDoubleClicked(room);
    }

    private void OnRoomClicked(MudClient.Core.Map.MapRoom? room)
    {
        if (_viewModel is not null)
        {
            _viewModel.SelectedRoom = room;
        }
    }

    private void OnManualNavigation()
    {
        if (_viewModel is not null)
        {
            _viewModel.FollowPlayer = false;
        }
    }

    private void OnRegionSelected(MudClient.Core.Map.FarmRegion region)
    {
        _viewModel?.NotifyAutoFarmRegionDrawn(region);
    }

    private void OnMovementKeyPressed(string direction)
    {
        _viewModel?.MainViewModel?.SendMapMovementCommand(direction);
    }

    private async void SearchRoom_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_viewModel is not { } viewModel || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var vnum = await SearchRoomAsync(owner);
        if (string.IsNullOrWhiteSpace(vnum))
        {
            return;
        }

        if (viewModel.FocusRoomByVnum(vnum) is null)
        {
            viewModel.MainViewModel?.AddToast($"Nie znaleziono pokoju o numerze {vnum}.", "error");
        }
    }

    private async void RoomNote_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_viewModel is not { } viewModel || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var note = await RoomNoteAsync(owner, viewModel.SelectedRoomNote);
        if (note is null)
        {
            return; // Cancelled — leave the room's note untouched.
        }

        viewModel.SetNoteOnSelectedRoom(note);
    }

    private async void SearchTeacher_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (_viewModel is not { } viewModel || TopLevel.GetTopLevel(this) is not Window owner)
        {
            return;
        }

        var name = await SearchTeacherAsync(owner, viewModel.SearchEntries);
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (viewModel.FocusSearchResultByName(name) is null)
        {
            viewModel.MainViewModel?.AddToast($"Nie znaleziono „{name}”.", "error");
        }
    }

    private void OnCenterRequested()
    {
        if (_viewModel?.CurrentRoom is { } room)
        {
            MapControl.CenterOnRoom(room);
        }
    }

    private void OnCenterOnRoomRequested(MudClient.Core.Map.MapRoom room)
    {
        MapControl.CenterOnRoom(room);
    }

    // ========================================================================
    // Autowalk locations / death marks — moved here from the former Autowalk
    // panel; the underlying state and commands still live on MainWindowViewModel
    // (see MapViewModel.MainViewModel), shared with TerminalOverlayCard's copy
    // of this same settings flyout.
    // ========================================================================

    private void GoToLocation_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button button &&
            button.DataContext is AutowalkLocation location &&
            _viewModel?.MainViewModel is { } mainViewModel)
        {
            mainViewModel.GoToLocationCommand.Execute(location);
        }
    }

    private void GoToDeath_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button button &&
            button.DataContext is DeathMarkEntry entry &&
            _viewModel?.MainViewModel is { } mainViewModel)
        {
            mainViewModel.GoToDeathCommand.Execute(entry);
        }
    }

    private void DeleteDeath_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button button &&
            button.DataContext is DeathMarkEntry entry &&
            _viewModel?.MainViewModel is { } mainViewModel)
        {
            mainViewModel.DeleteDeathCommand.Execute(entry);
        }
    }

    private async void DeleteLocation_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button button &&
            button.DataContext is AutowalkLocation location &&
            _viewModel?.MainViewModel is { } mainViewModel)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner)
            {
                return;
            }

            button.IsEnabled = false;
            try
            {
                if (await ConfirmDeletionAsync(owner, "cel autowalk", location.Name))
                {
                    mainViewModel.DeleteLocationCommand.Execute(location);
                }
            }
            finally
            {
                button.IsEnabled = true;
            }
        }
    }
}
