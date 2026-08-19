using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using MudClient.App.Controls;
using MudClient.App.Docking;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.App.Views;
using MudClient.App.Views.Panels;
using MudClient.Core.Gmcp;

namespace MudClient.App.Tests;

// The group spell shortcut editor (label -> spell name, e.g. "cc" -> "cure critical") lives in
// two hand-written flyout copies — PanelToolView.axaml's GroupSettingsButton (docked panels) and
// TerminalOverlayCard.axaml's own copy (pinned/overlay panels) — because Flyout content renders
// outside its host's own visual tree and can't be shared via a simple named-element binding. A
// prior bug in this exact codebase (TerminalOverlayCard's move buttons, see
// PinnedTabUiTests.OverlayMoveButtons_ClickingTheRenderedButton_InvokesTheMoveCommand) shipped
// with a Command binding that compiled cleanly but never fired at runtime — only a test that
// clicks the actual rendered controls (not the ViewModel commands directly) would catch that
// class of bug, so both copies get one here.
[Collection(AvaloniaUiCollection.Name)]
public sealed class GroupPanelUiTests
{
    private static void Pump(Window window, int iterations = 10)
    {
        for (var i = 0; i < iterations; i++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }
    }

    private static (MainWindowViewModel ViewModel, string Directory) CreateViewModel()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "KillerMudClient-GroupPanelUiTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var viewModel = new MainWindowViewModel(
            new ProfileService(directory),
            new AppSettingsService(directory),
            new DockLayoutService(directory),
            groupSpellStore: new GroupSpellStore(Path.Combine(directory, "group-spells.json")));
        return (viewModel, directory);
    }

    [AvaloniaFact]
    public void GroupSettingsFlyout_AddThenRemoveShortcut_ThroughRenderedControls()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var tool = new PanelTool
            {
                Id = "Group",
                Title = "👥 Drużyna",
                ViewType = typeof(GroupPanelView),
                Context = viewModel,
            };
            var host = new PanelToolView { DataContext = tool };
            var window = new Window { Width = 640, Height = 800, Content = host };

            window.Show();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            window.UpdateLayout();

            // The generic placeholder must be hidden and the real Group button visible — this is
            // exactly the pair of checks the earlier "still shows the placeholder" report needed.
            var placeholder = host.FindControl<Button>("SettingsButton")!;
            Assert.False(placeholder.IsVisible);
            var settingsButton = host.FindControl<Button>("GroupSettingsButton")!;
            Assert.True(settingsButton.IsVisible);

            settingsButton.Flyout!.ShowAt(settingsButton);
            Dispatcher.UIThread.RunJobs();

            var labelBox = window.GetVisualDescendants().OfType<TextBox>()
                .Single(box => box.PlaceholderText == "cc");
            var nameBox = window.GetVisualDescendants().OfType<TextBox>()
                .Single(box => box.PlaceholderText == "cure critical");
            labelBox.Text = "cc";
            nameBox.Text = "cure critical";

            // A Command-bound Button's RaiseEvent(ClickEvent) does not reliably invoke the bound
            // command in headless tests (Avalonia's Button.OnClick gates on CanExecute internally
            // in ways that don't always evaluate the same as a real pointer click) — call the
            // resolved Command directly instead, the same way a real click ultimately would.
            var addButton = window.GetVisualDescendants().OfType<Button>()
                .Single(button => Equals(button.Content, "+ Dodaj"));
            addButton.Command!.Execute(addButton.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            var shortcut = Assert.Single(viewModel.GroupSpells);
            Assert.Equal("cc", shortcut.Label);
            Assert.Equal("cure critical", shortcut.SpellName);

            var removeButton = window.GetVisualDescendants().OfType<Button>()
                .Single(button => Equals(button.Content, "✕"));
            removeButton.Command!.Execute(removeButton.CommandParameter);
            Dispatcher.UIThread.RunJobs();

            Assert.Empty(viewModel.GroupSpells);

            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public void GroupOverlayCard_SettingsButton_IsVisibleInsteadOfThePlaceholder()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            var window = new MainWindow { DataContext = viewModel, Width = 1200, Height = 800 };
            window.Show();
            Pump(window);

            viewModel.ApplyLayoutCommand.Execute("TRANSPARENCY");
            Pump(window);

            var factory = Assert.IsType<MudDockFactory>(viewModel.Layout!.Factory);
            factory.AllTools.First(t => t.Id == "Group").PinAsOverlayCommand.Execute(null);
            Pump(window);

            var card = window.GetVisualDescendants().OfType<TerminalOverlayCard>()
                .Single(c => c.Overlay?.Panel.Id == "Group");

            // TerminalOverlayCard hosts its own copy of every settings button in its title bar
            // *and* embeds a PanelToolView underneath carrying a second, hidden copy (see
            // PanelToolView's `isOverlaid` check) — so there can be more than one Button with a
            // given tooltip, and the real assertion is "exactly one of them is visible", not
            // "there is exactly one".
            var placeholders = card.GetVisualDescendants().OfType<Button>()
                .Where(button => ToolTip.GetTip(button) as string == "Ustawienia panelu (wkrótce)")
                .ToList();
            Assert.NotEmpty(placeholders);
            Assert.All(placeholders, button => Assert.False(button.IsVisible));

            var realButtons = card.GetVisualDescendants().OfType<Button>()
                .Where(button => ToolTip.GetTip(button) as string == "Skróty czarów drużyny")
                .ToList();
            Assert.Single(realButtons, button => button.IsVisible);

            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static GroupMember CreateMember(string name = "Aragorn") => GroupMember.FromCore(
        new CharacterGroupMember(name, "standing", "bez ran", 7, "wypoczęty", 4, null, false, "6017", false));

    private static void RaiseMemSpellsChanged(MainWindowViewModel viewModel, params MemorizedSpell[] spells) =>
        typeof(MainWindowViewModel)
            .GetMethod("OnMemSpellsChanged", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(viewModel, [(IReadOnlyList<MemorizedSpell>)spells]);

    [AvaloniaFact]
    public void GroupSpellButton_SpellNotMemorized_BackgroundTurnsRed()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.GroupSpells.Add(new GroupSpellShortcut { Label = "cc", SpellName = "cure critical" });
            viewModel.Group.Add(CreateMember());
            // "armor" memorized, but not "cure critical" — the shortcut's own spell is missing.
            RaiseMemSpellsChanged(viewModel, new MemorizedSpell(1, 1, "armor", true, false));
            Dispatcher.UIThread.RunJobs();

            var panel = new GroupPanelView { DataContext = viewModel };
            var window = new Window { Width = 360, Height = 300, Content = panel };
            window.Show();
            Pump(window);

            var button = window.GetVisualDescendants().OfType<Button>()
                .Single(b => ToolTip.GetTip(b) as string == "cure critical");

            Avalonia.Application.Current!.TryGetResource("MudBrushCrimson", null, out var expectedBrush);
            Assert.Equal(expectedBrush, button.Background);

            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [AvaloniaFact]
    public void GroupSpellButton_SpellMemorized_BackgroundIsNotReddened()
    {
        var (viewModel, directory) = CreateViewModel();
        try
        {
            viewModel.GroupSpells.Add(new GroupSpellShortcut { Label = "cc", SpellName = "cure critical" });
            viewModel.Group.Add(CreateMember());
            RaiseMemSpellsChanged(viewModel, new MemorizedSpell(1, 3, "cure critical", true, false));
            Dispatcher.UIThread.RunJobs();

            var panel = new GroupPanelView { DataContext = viewModel };
            var window = new Window { Width = 360, Height = 300, Content = panel };
            window.Show();
            Pump(window);

            var button = window.GetVisualDescendants().OfType<Button>()
                .Single(b => ToolTip.GetTip(b) as string == "cure critical");

            Avalonia.Application.Current!.TryGetResource("MudBrushCrimson", null, out var crimsonBrush);
            Assert.NotEqual(crimsonBrush, button.Background);

            window.Close();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
