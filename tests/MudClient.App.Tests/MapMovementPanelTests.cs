using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MudClient.App.ViewModels;
using MudClient.App.Views;
using MudClient.App.Views.Panels;
using MudClient.Core.Map;

namespace MudClient.App.Tests;

[Collection(AvaloniaUiCollection.Name)]
public sealed class MapMovementPanelTests
{
    [Fact]
    public void UpdateRoomExits_ReplacesAllSixDirectionSlots()
    {
        using var viewModel = CreateViewModel();
        viewModel.UpdateRoomExits(
        [
            new("n", null, false, false),
            new("S", null, false, false),
            new("w", null, false, false),
            new("E", "brama", true, true),
            new("u", null, false, false),
            new("D", null, false, false),
            new("NE", null, false, false),
        ]);

        Assert.NotNull(viewModel.NorthExit);
        Assert.NotNull(viewModel.SouthExit);
        Assert.NotNull(viewModel.WestExit);
        Assert.Equal("brama", viewModel.EastExit?.Name);
        Assert.Equal("brama", viewModel.EastExitName);
        Assert.True(viewModel.EastExitIsClosedDoor);
        Assert.NotNull(viewModel.UpExit);
        Assert.NotNull(viewModel.DownExit);
        Assert.True(viewModel.HasMovementExits);

        viewModel.UpdateRoomExits([new RoomExitInfo("D", null, false, false)]);

        Assert.Null(viewModel.NorthExit);
        Assert.Null(viewModel.SouthExit);
        Assert.Null(viewModel.WestExit);
        Assert.Null(viewModel.EastExit);
        Assert.Null(viewModel.EastExitName);
        Assert.False(viewModel.EastExitIsClosedDoor);
        Assert.Null(viewModel.UpExit);
        Assert.NotNull(viewModel.DownExit);
    }

    [AvaloniaFact]
    public void MovementButtons_StayVisibleAndFollowLatestRoomInfoAvailability()
    {
        using var viewModel = CreateViewModel();
        viewModel.UpdateRoomExits(
        [
            new("N", null, false, false),
            new("E", "brama", true, true),
            new("U", null, false, false),
        ]);
        var panel = new MapPanelView { DataContext = viewModel };
        var window = new Window { Width = 800, Height = 700, Content = panel };

        try
        {
            window.Show();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

            Assert.True(FindButton(panel, "MoveNorthButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveEastButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveUpButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveSouthButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveWestButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveDownButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveNorthButton").IsEffectivelyEnabled);
            Assert.True(FindButton(panel, "MoveEastButton").IsEffectivelyEnabled);
            Assert.True(FindButton(panel, "MoveUpButton").IsEffectivelyEnabled);
            Assert.False(FindButton(panel, "MoveSouthButton").IsEffectivelyEnabled);
            Assert.False(FindButton(panel, "MoveWestButton").IsEffectivelyEnabled);
            Assert.False(FindButton(panel, "MoveDownButton").IsEffectivelyEnabled);
            Assert.Contains(
                FindButton(panel, "MoveUpButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "↑ U");
            Assert.Contains(
                FindButton(panel, "MoveDownButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "↓ D");
            Assert.Contains(
                FindButton(panel, "MoveNorthButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "8");
            Assert.Contains(
                FindButton(panel, "MoveSouthButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "2");
            Assert.Contains(
                FindButton(panel, "MoveWestButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "4");
            Assert.Contains(
                FindButton(panel, "MoveEastButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "6");
            Assert.Contains(
                FindButton(panel, "MoveUpButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "9");
            Assert.Contains(
                FindButton(panel, "MoveDownButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "3");
            Assert.Contains(
                FindButton(panel, "MoveEastButton").GetVisualDescendants().OfType<TextBlock>(),
                text => text.Text == "brama" && text.IsEffectivelyVisible);
            Assert.True(FindLock(panel, "MoveEastButton").IsEffectivelyVisible);
            Assert.False(FindLock(panel, "MoveNorthButton").IsEffectivelyVisible);

            viewModel.UpdateRoomExits([new RoomExitInfo("D", null, false, false)]);
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();

            Assert.True(FindButton(panel, "MoveNorthButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveEastButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveUpButton").IsEffectivelyVisible);
            Assert.True(FindButton(panel, "MoveDownButton").IsEffectivelyVisible);
            Assert.False(FindButton(panel, "MoveNorthButton").IsEffectivelyEnabled);
            Assert.False(FindButton(panel, "MoveEastButton").IsEffectivelyEnabled);
            Assert.False(FindButton(panel, "MoveUpButton").IsEffectivelyEnabled);
            Assert.True(FindButton(panel, "MoveDownButton").IsEffectivelyEnabled);
            Assert.False(FindLock(panel, "MoveEastButton").IsEffectivelyVisible);
        }
        finally
        {
            window.Close();
        }
    }

    [AvaloniaFact]
    public void MovementSection_KeepsFixedHeightAndNorthIsDirectlyAboveSouth()
    {
        using var viewModel = CreateViewModel();
        var panel = new MapPanelView { DataContext = viewModel };
        var window = new Window { Width = 800, Height = 700, Content = panel };

        try
        {
            window.Show();
            window.UpdateLayout();

            var section = panel.GetVisualDescendants().OfType<Border>()
                .Single(border => border.Name == "MovementSection");
            var north = FindButton(panel, "MoveNorthButton");
            var south = FindButton(panel, "MoveSouthButton");

            Assert.True(section.IsEffectivelyVisible);
            Assert.Equal(108, section.Bounds.Height);
            Assert.Equal(Grid.GetColumn(north), Grid.GetColumn(south));
            Assert.Equal(Grid.GetRow(north) + 1, Grid.GetRow(south));
        }
        finally
        {
            window.Close();
        }
    }

    [Theory]
    [InlineData(Key.NumPad8, "N")]
    [InlineData(Key.NumPad2, "S")]
    [InlineData(Key.NumPad4, "W")]
    [InlineData(Key.NumPad6, "E")]
    [InlineData(Key.NumPad9, "U")]
    [InlineData(Key.NumPad3, "D")]
    public void NumpadMapping_UsesMovementButtonDirections(Key key, string expected)
    {
        Assert.Equal(expected, MainWindow.GetNumpadMovementDirection(key, KeyModifiers.None));
    }

    [Fact]
    public void NumpadMapping_RejectsModifiersAndUnassignedKeys()
    {
        Assert.Null(MainWindow.GetNumpadMovementDirection(Key.NumPad8, KeyModifiers.Control));
        Assert.Null(MainWindow.GetNumpadMovementDirection(Key.NumPad5, KeyModifiers.None));
    }

    [Theory]
    [InlineData(Key.NumPad1, true)]
    [InlineData(Key.NumPad2, true)]
    [InlineData(Key.NumPad7, true)]
    [InlineData(Key.NumPad9, true)]
    [InlineData(Key.NumPad5, false)]
    [InlineData(Key.Enter, false)]
    public void ReservedNumpadKeys_BlockMovementDigitsButNotEnter(Key key, bool expected)
    {
        Assert.Equal(expected, MainWindow.IsReservedNumpadMovementKey(key, KeyModifiers.None));
    }

    private static MapViewModel CreateViewModel() =>
        new(AppContext.BaseDirectory, new GmcpLocationResolver());

    private static Button FindButton(MapPanelView panel, string name) =>
        panel.GetVisualDescendants().OfType<Button>().Single(button => button.Name == name);

    private static TextBlock FindLock(MapPanelView panel, string buttonName) =>
        FindButton(panel, buttonName).GetVisualDescendants().OfType<TextBlock>()
            .Single(text => text.Text == "🔒");
}
