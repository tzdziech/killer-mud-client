using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Gmcp;

namespace MudClient.App.Tests;

/// <summary>
/// Covers the ready/not-ready counting that drives the "[gotowe/niegotowe]" bracket shown after a
/// buff's name in the "Buffy" section (MemSpellsPanelView.axaml) — fed by Char.MemSpell GMCP and
/// applied to every tracked <see cref="MudClient.App.Models.BuffWatchEntry"/> in
/// MainWindowViewModel.OnMemSpellsChanged/UpdateBuffMemoStatus. Uses the real production event
/// path (CharacterStateResolver.Process, reached via reflection like
/// AffectsChangedEvent_IsSubscribedByConstructor in MainWindowViewModelTests) instead of
/// replicating the handler logic, since that logic itself is what's under test here.
/// </summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class BuffMemSpellCountTests : IAsyncDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "MudClientTests", Guid.NewGuid().ToString("N"));

    private readonly MainWindowViewModel _vm;
    private readonly CharacterStateResolver _characterState;

    public BuffMemSpellCountTests()
    {
        Directory.CreateDirectory(_tempDir);
        _vm = new MainWindowViewModel(
            profileService: new ProfileService(_tempDir),
            settingsService: new AppSettingsService(_tempDir));

        var field = typeof(MainWindowViewModel).GetField(
            "_characterState", BindingFlags.NonPublic | BindingFlags.Instance);
        _characterState = (CharacterStateResolver)field!.GetValue(_vm)!;
    }

    public async ValueTask DisposeAsync()
    {
        await _vm.DisposeAsync();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private void SendMemSpells(string json)
    {
        _characterState.Process(new GmcpMessage("Char.MemSpell", json));
        Dispatcher.UIThread.RunJobs();
    }

    [AvaloniaFact]
    public void MemSpellReceived_CountsMemoizedAndUsedByName()
    {
        _vm.NewBuffName = "shield";
        _vm.AddBuffCommand.Execute(null);

        SendMemSpells("""
            [
              { "counter": 1, "circle": 1, "name": "shield", "memed": true, "meming": false },
              { "counter": 2, "circle": 1, "name": "shield", "memed": true, "meming": false },
              { "counter": 3, "circle": 1, "name": "shield", "memed": false, "meming": false }
            ]
            """);

        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name == "shield");
        Assert.Equal(2, buff.MemoizedCount);
        Assert.Equal(1, buff.UsedCount);
        Assert.True(buff.IsListedInMemSpell);
        Assert.True(buff.IsReadyBracketGreen);
        Assert.Equal("[2/1]", buff.BracketAfterText);
    }

    [AvaloniaFact]
    public void MemSpellReceived_MemingSlot_CountsAsUsed()
    {
        _vm.NewBuffName = "armor";
        _vm.AddBuffCommand.Execute(null);

        SendMemSpells("""
            [
              { "counter": 1, "circle": 1, "name": "armor", "memed": false, "meming": true }
            ]
            """);

        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name == "armor");
        Assert.Equal(0, buff.MemoizedCount);
        Assert.Equal(1, buff.UsedCount);
        Assert.True(buff.IsListedInMemSpell);
        Assert.False(buff.IsReadyBracketGreen);
    }

    [AvaloniaFact]
    public void MemSpellReceived_NameAbsentFromList_NotListedAndZeroCounts()
    {
        _vm.NewBuffName = "bull strength";
        _vm.AddBuffCommand.Execute(null);

        SendMemSpells("""
            [
              { "counter": 1, "circle": 1, "name": "armor", "memed": true, "meming": false }
            ]
            """);

        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name == "bull strength");
        Assert.Equal(0, buff.MemoizedCount);
        Assert.Equal(0, buff.UsedCount);
        Assert.False(buff.IsListedInMemSpell);
    }

    [AvaloniaFact]
    public void MemSpellReceived_MatchesAcrossWhitespaceInUserTypedName()
    {
        _vm.NewBuffName = "  mirror image  ";
        _vm.AddBuffCommand.Execute(null);

        SendMemSpells("""
            [
              { "counter": 1, "circle": 3, "name": "mirror image", "memed": true, "meming": false }
            ]
            """);

        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name.Trim() == "mirror image");
        Assert.Equal(1, buff.MemoizedCount);
        Assert.True(buff.IsListedInMemSpell);
    }

    [AvaloniaFact]
    public void MemSpellReceived_UpdatesCountsForBuffsInEveryBuffSet_NotJustSelected()
    {
        _vm.NewBuffName = "armor";
        _vm.AddBuffCommand.Execute(null);
        var defaultSet = _vm.SelectedBuffSet;

        _vm.NewBuffSetName = "Walka";
        _vm.CreateBuffSetCommand.Execute(null);
        _vm.NewBuffName = "sanctuary";
        _vm.AddBuffCommand.Execute(null);

        // "Walka" is now selected; "armor" lives in the unselected default set.
        SendMemSpells("""
            [
              { "counter": 1, "circle": 1, "name": "armor", "memed": true, "meming": false }
            ]
            """);

        var armorBuff = defaultSet!.Buffs.Single(b => b.Name == "armor");
        Assert.Equal(1, armorBuff.MemoizedCount);
        Assert.True(armorBuff.IsListedInMemSpell);
    }

    [AvaloniaFact]
    public void MemSpellReceived_SecondUpdateReplacesPreviousCounts()
    {
        _vm.NewBuffName = "blur";
        _vm.AddBuffCommand.Execute(null);

        SendMemSpells("""
            [{ "counter": 1, "circle": 1, "name": "blur", "memed": true, "meming": false }]
            """);
        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name == "blur");
        Assert.Equal(1, buff.MemoizedCount);

        SendMemSpells("[]");
        Assert.Equal(0, buff.MemoizedCount);
        Assert.Equal(0, buff.UsedCount);
        Assert.False(buff.IsListedInMemSpell);
    }

    [AvaloniaFact]
    public void AddBuff_AfterMemSpellsAlreadyKnown_AppliesCountsImmediately()
    {
        SendMemSpells("""
            [{ "counter": 1, "circle": 1, "name": "fly", "memed": true, "meming": false }]
            """);

        _vm.NewBuffName = "fly";
        _vm.AddBuffCommand.Execute(null);

        var buff = Assert.Single(_vm.RequiredBuffs, b => b.Name == "fly");
        Assert.Equal(1, buff.MemoizedCount);
        Assert.True(buff.IsListedInMemSpell);
    }
}
