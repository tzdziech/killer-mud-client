using System.Reflection;
using System.Text.Json;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Map;

namespace MudClient.App.Tests;

/// <summary>Covers <see cref="MainWindowViewModel"/>'s vnum echo — splicing " [vnum: N]" onto the
/// end of the new room's own name line when the map already knows that room's name
/// (<c>OnRoomEnterShowVnum</c> + <c>AnnotateRoomVnum</c>), or falling back to a standalone
/// synthetic line for an unmapped room. Reflection-invokes the private handlers directly, the same
/// approach <see cref="AutowalkArrivalTests"/> uses for its sibling handler.</summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class RoomEnterVnumEchoTests
{
    private static void InvokeOnRoomEnterShowVnum(MainWindowViewModel viewModel, string vnum) =>
        typeof(MainWindowViewModel)
            .GetMethod("OnRoomEnterShowVnum", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, [vnum]);

    private static void InvokeOnTextReceived(MainWindowViewModel viewModel, string text) =>
        typeof(MainWindowViewModel)
            .GetMethod("OnTextReceived", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, [text]);

    private static void SetConnected(MainWindowViewModel viewModel, bool connected) =>
        typeof(MainWindowViewModel)
            .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(viewModel, connected);

    private static void SetMapIndex(MainWindowViewModel viewModel, MapIndex index) =>
        typeof(MapViewModel).GetProperty(nameof(MapViewModel.MapIndex))!.SetValue(viewModel.Map, index);

    private static MapIndex CreateIndexWithRoom(int id, string vnum, string name) => new(new MapDocument
    {
        Areas =
        [
            new MapArea
            {
                Id = 1,
                Rooms =
                [
                    new MapRoom
                    {
                        Id = id,
                        AreaId = 1,
                        Name = name,
                        Coordinates = new MapCoordinates(0, 0, 0),
                        UserData = new Dictionary<string, JsonElement>
                        {
                            ["vnum"] = JsonSerializer.SerializeToElement(vnum),
                        },
                    },
                ],
            },
        ],
    });

    private static async Task<(MainWindowViewModel ViewModel, string Directory)> CreateViewModelAsync()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_RoomEnterVnumEchoTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return (new MainWindowViewModel(settingsService: new AppSettingsService(directory)), directory);
    }

    [AvaloniaFact]
    public async Task RoomEnter_UnmappedRoom_FallsBackToStandaloneLine()
    {
        var (viewModel, directory) = await CreateViewModelAsync();
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            SetConnected(viewModel, true);
            InvokeOnRoomEnterShowVnum(viewModel, "6017");
            // The unmapped-room fallback marshals its echo via Dispatcher.UIThread.Post (it fires
            // from GMCP processing on the network thread, not the UI thread) — drain the queue.
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(output, line => line.Contains("[vnum: 6017]"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task RoomEnter_WhileDisconnected_DoesNotEchoAnything()
    {
        var (viewModel, directory) = await CreateViewModelAsync();
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            SetConnected(viewModel, false);
            InvokeOnRoomEnterShowVnum(viewModel, "6017");

            Assert.Empty(output);
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task RoomEnter_MappedRoom_SplicesVnumOntoTheRoomNameLine()
    {
        var (viewModel, directory) = await CreateViewModelAsync();
        SetMapIndex(viewModel, CreateIndexWithRoom(1, "100", "Ciemny las"));
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            SetConnected(viewModel, true);
            InvokeOnRoomEnterShowVnum(viewModel, "100");
            InvokeOnTextReceived(viewModel, "Ciemny las\nWokolo widzisz same drzewa.\n");
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(output, line => line.Contains("Ciemny las [vnum: 100]"));
            Assert.DoesNotContain(output, line => line.TrimEnd('\r', '\n') == "Ciemny las");
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task RoomEnter_MappedRoom_MatchesAgainstAnsiColoredNameLine()
    {
        var (viewModel, directory) = await CreateViewModelAsync();
        SetMapIndex(viewModel, CreateIndexWithRoom(1, "100", "Ciemny las"));
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            SetConnected(viewModel, true);
            InvokeOnRoomEnterShowVnum(viewModel, "100");
            InvokeOnTextReceived(viewModel, "[32mCiemny las[0m\nOpis.\n");
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(output, line => line.Contains("[vnum: 100]") && line.Contains("Ciemny las"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public async Task RoomEnter_MappedRoom_NameNeverArrives_NextRoomEntrySupersedesIt()
    {
        var (viewModel, directory) = await CreateViewModelAsync();
        SetMapIndex(viewModel, CreateIndexWithRoom(1, "100", "Ciemny las"));
        var output = new List<string>();
        viewModel.OutputReceived += text => output.Add(text);

        try
        {
            SetConnected(viewModel, true);
            InvokeOnRoomEnterShowVnum(viewModel, "100");
            // A GMCP prompt with no matching room text — the pending vnum stays queued rather
            // than attaching to unrelated content...
            InvokeOnTextReceived(viewModel, "Cos zupelnie innego.\n");
            // ...but a new room supersedes it outright rather than leaking across entries.
            InvokeOnRoomEnterShowVnum(viewModel, "6017");
            Dispatcher.UIThread.RunJobs();

            Assert.Contains(output, line => line.Contains("[vnum: 6017]"));
            Assert.DoesNotContain(output, line => line.Contains("[vnum: 100]"));
        }
        finally
        {
            await viewModel.DisposeAsync();
            Directory.Delete(directory, recursive: true);
        }
    }
}
