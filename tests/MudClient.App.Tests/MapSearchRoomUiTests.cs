using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views.Panels;
using MudClient.Core.Map;
using Xunit;

namespace MudClient.App.Tests;

[Collection(AvaloniaUiCollection.Name)]
public sealed class MapSearchRoomUiTests
{
    [AvaloniaFact]
    public async Task SearchRoom_KnownVnum_FocusesRoomWithoutToast()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchRoomFound_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchRoomAsync = _ => Task.FromResult<string?>("100"),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchRoomOnClick(panel);

        Assert.Equal("100", viewModel.Map.SelectedRoom?.Vnum);
        Assert.Null(viewModel.Map.ErrorMessage);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task SearchRoom_UnknownVnum_ShowsToastAndLeavesSelectionUnchanged()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchRoomNotFound_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchRoomAsync = _ => Task.FromResult<string?>("999"),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchRoomOnClick(panel);

        Assert.Null(viewModel.Map.SelectedRoom);
        Assert.Contains(viewModel.Toasts, toast => toast.Text.Contains("999"));

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task SearchRoom_CancelledDialog_DoesNothing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchRoomCancelled_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchRoomAsync = _ => Task.FromResult<string?>(null),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchRoomOnClick(panel);

        Assert.Null(viewModel.Map.SelectedRoom);
        Assert.DoesNotContain(viewModel.Toasts, toast => toast.Text.Contains("Nie znaleziono"));

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    private static Task InvokeSearchRoomOnClick(MapPanelView panel)
    {
        var method = typeof(MapPanelView).GetMethod("SearchRoom_OnClick",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return (Task)(method!.Invoke(panel, [null, null]) is Task task ? task : Task.CompletedTask);
    }

    private static MapIndex CreateSampleIndex()
    {
        var room = new MapRoom
        {
            Id = 1,
            AreaId = 1,
            Name = "Test Room",
            Coordinates = new MapCoordinates(10, 20, 5),
            UserData = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["vnum"] = System.Text.Json.JsonSerializer.SerializeToElement("100"),
                ["sector"] = System.Text.Json.JsonSerializer.SerializeToElement("inside"),
            },
        };
        var area = new MapArea
        {
            Id = 1,
            Name = "Test Area",
            Rooms = [room],
        };
        var doc = new MapDocument
        {
            Areas = [area],
        };
        return new MapIndex(doc);
    }

    private static void SetMapIndex(MapViewModel vm, MapIndex index)
    {
        var property = typeof(MapViewModel).GetProperty(nameof(MapViewModel.MapIndex));
        Assert.NotNull(property);
        property!.SetValue(vm, index);
    }
}
