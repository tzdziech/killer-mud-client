using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views.Panels;
using MudClient.Core.Map;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Covers MapPanelView's "Notatka..." right-click menu item — the code-behind's dialog
/// interaction, mirroring MapSearchRoomUiTests' conventions (invoking the Click handler directly
/// via reflection rather than driving a real ContextMenu-open gesture). The underlying
/// MapViewModel.SetNoteOnSelectedRoom logic itself is covered by MapViewModelTests.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class RoomNoteUiTests
{
    [AvaloniaFact]
    public async Task RoomNote_TypedText_SavesItOnTheSelectedRoom()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_RoomNoteSave_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(new ProfileService(directory), new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());
        viewModel.Map.SelectedRoom = viewModel.Map.MapIndex!.FindFirstRoomByVnum("100");

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            RoomNoteAsync = (_, _) => Task.FromResult<string?>("uważaj na zapadnię"),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeRoomNoteOnClick(panel);

        Assert.Equal("uważaj na zapadnię", viewModel.Map.SelectedRoomNote);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task RoomNote_CancelledDialog_LeavesTheNoteUnchanged()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_RoomNoteCancel_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(new ProfileService(directory), new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());
        viewModel.Map.SelectedRoom = viewModel.Map.MapIndex!.FindFirstRoomByVnum("100");
        viewModel.Map.SetNoteOnSelectedRoom("oryginalna notatka");

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            RoomNoteAsync = (_, _) => Task.FromResult<string?>(null), // simulates Anuluj
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeRoomNoteOnClick(panel);

        Assert.Equal("oryginalna notatka", viewModel.Map.SelectedRoomNote);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task RoomNote_EmptyStringSaved_ClearsAnExistingNote()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_RoomNoteClear_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(new ProfileService(directory), new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());
        viewModel.Map.SelectedRoom = viewModel.Map.MapIndex!.FindFirstRoomByVnum("100");
        viewModel.Map.SetNoteOnSelectedRoom("do wyczyszczenia");

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            RoomNoteAsync = (_, _) => Task.FromResult<string?>(string.Empty), // simulates Zapisz with an empty box
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeRoomNoteOnClick(panel);

        Assert.Null(viewModel.Map.SelectedRoomNote);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task RoomNote_DialogIsPrefilledWithTheRoomsCurrentNote()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_RoomNotePrefill_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(new ProfileService(directory), new AppSettingsService(directory));
        SetMapIndex(viewModel.Map, CreateSampleIndex());
        viewModel.Map.SelectedRoom = viewModel.Map.MapIndex!.FindFirstRoomByVnum("100");
        viewModel.Map.SetNoteOnSelectedRoom("już tu jest");

        string? receivedInitialNote = "not called";
        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            RoomNoteAsync = (_, initial) =>
            {
                receivedInitialNote = initial;
                return Task.FromResult<string?>(null);
            },
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeRoomNoteOnClick(panel);

        Assert.Equal("już tu jest", receivedInitialNote);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    private static Task InvokeRoomNoteOnClick(MapPanelView panel)
    {
        var method = typeof(MapPanelView).GetMethod("RoomNote_OnClick",
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
        var area = new MapArea { Id = 1, Name = "Test Area", Rooms = [room] };
        return new MapIndex(new MapDocument { Areas = [area] });
    }

    private static void SetMapIndex(MapViewModel vm, MapIndex index)
    {
        var property = typeof(MapViewModel).GetProperty(nameof(MapViewModel.MapIndex));
        Assert.NotNull(property);
        property!.SetValue(vm, index);
    }
}
