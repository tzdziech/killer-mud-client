using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views.Panels;
using MudClient.Core.Map;
using Xunit;

namespace MudClient.App.Tests;

[Collection(AvaloniaUiCollection.Name)]
public sealed class MapSearchTeacherUiTests
{
    [AvaloniaFact]
    public async Task SearchTeacher_KnownName_FocusesRoomWithoutToast()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchTeacherFound_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var teacher = new TeacherEntry("1", "Mistrz Moran", "Region", null, "100", [], [], []);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetTeacherCatalog(viewModel.Map, [teacher]);
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchTeacherAsync = _ => Task.FromResult<string?>("Mistrz Moran"),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchTeacherOnClick(panel);

        Assert.Equal("100", viewModel.Map.SelectedRoom?.Vnum);

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task SearchTeacher_UnknownName_ShowsToastAndLeavesSelectionUnchanged()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchTeacherNotFound_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var teacher = new TeacherEntry("1", "Mistrz Moran", "Region", null, "100", [], [], []);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetTeacherCatalog(viewModel.Map, [teacher]);
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchTeacherAsync = _ => Task.FromResult<string?>("Nieznajomy"),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchTeacherOnClick(panel);

        Assert.Null(viewModel.Map.SelectedRoom);
        Assert.Contains(viewModel.Toasts, toast => toast.Text.Contains("Nieznajomy"));

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    [AvaloniaFact]
    public async Task SearchTeacher_CancelledDialog_DoesNothing()
    {
        var directory = Path.Combine(Path.GetTempPath(), "KillerMudClient_SearchTeacherCancelled_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var teacher = new TeacherEntry("1", "Mistrz Moran", "Region", null, "100", [], [], []);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory));
        SetTeacherCatalog(viewModel.Map, [teacher]);
        SetMapIndex(viewModel.Map, CreateSampleIndex());

        var panel = new MapPanelView
        {
            DataContext = viewModel.Map,
            SearchTeacherAsync = _ => Task.FromResult<string?>(null),
        };
        var window = new Window { Width = 520, Height = 720, Content = panel };
        window.Show();

        await InvokeSearchTeacherOnClick(panel);

        Assert.Null(viewModel.Map.SelectedRoom);
        Assert.DoesNotContain(viewModel.Toasts, toast => toast.Text.Contains("Nie znaleziono"));

        window.Close();
        await viewModel.DisposeAsync();
        Directory.Delete(directory, recursive: true);
    }

    private static Task InvokeSearchTeacherOnClick(MapPanelView panel)
    {
        var method = typeof(MapPanelView).GetMethod("SearchTeacher_OnClick",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return (Task)(method!.Invoke(panel, [null, null]) is Task task ? task : Task.CompletedTask);
    }

    private static void SetTeacherCatalog(MapViewModel vm, IReadOnlyList<TeacherEntry> teachers)
    {
        var field = typeof(MapViewModel).GetField("_teacherCatalog", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        field!.SetValue(vm, teachers);
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
