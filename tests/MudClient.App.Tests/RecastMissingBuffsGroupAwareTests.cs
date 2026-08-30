using System.Reflection;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using MudClient.App.Models;
using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Gmcp;

namespace MudClient.App.Tests;

/// <summary>
/// Covers three additions to the "RZUĆ BRAKUJĄCE" ("recast missing buffs") flow
/// (MainWindowViewModel.RecastMissingBuffsAsync/SelectBuffsToCast):
/// 1. a buff isn't cast unless it's actually memorized (MemoizedCount > 0),
/// 2. "mass X" buffs match the plain "X" Char.Affects name the server sends for either version
///    (BuffWatchEntry.NormalizeAffectName), so both light up together,
/// 3. when a set has both "X" and "mass X", only one is actually cast — chosen by whether any
///    other (non-NPC) player shares the group.
/// Uses the real production event path (CharacterStateResolver.Process) for Char.Affects/
/// Char.Group, same pattern as BuffMemSpellCountTests.
/// </summary>
[Collection(AvaloniaUiCollection.Name)]
public sealed class RecastMissingBuffsGroupAwareTests : IAsyncDisposable
{
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), "MudClientTests", Guid.NewGuid().ToString("N"));

    private readonly MainWindowViewModel _vm;
    private readonly CharacterStateResolver _characterState;
    private readonly List<string> _output = [];

    public RecastMissingBuffsGroupAwareTests()
    {
        Directory.CreateDirectory(_tempDir);
        _vm = new MainWindowViewModel(
            profileService: new ProfileService(_tempDir),
            settingsService: new AppSettingsService(_tempDir));
        _vm.OutputReceived += _output.Add;

        var field = typeof(MainWindowViewModel).GetField(
            "_characterState", BindingFlags.NonPublic | BindingFlags.Instance);
        _characterState = (CharacterStateResolver)field!.GetValue(_vm)!;

        typeof(MainWindowViewModel)
            .GetField("_isConnected", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(_vm, true);
    }

    public async ValueTask DisposeAsync()
    {
        await _vm.DisposeAsync();
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private void Pump() => Dispatcher.UIThread.RunJobs();

    private void SendAffects(string json)
    {
        _characterState.Process(new GmcpMessage("Char.Affects", json));
        Pump();
    }

    private void SendGroup(string json)
    {
        _characterState.Process(new GmcpMessage("Char.Group", json));
        Pump();
    }

    private async Task RecastAsync()
    {
        await _vm.RecastBuffsCommand.ExecuteAsync(null);
        Pump();
    }

    private bool CastSent(string spellName) =>
        _output.Any(line => line.Contains($"> cast \"{spellName}\" self", StringComparison.Ordinal));

    // ====================================================================
    // 1. Memorized guard
    // ====================================================================

    [AvaloniaFact]
    public async Task RecastMissing_SkipsBuffNotMemorized()
    {
        _vm.NewBuffName = "armor";
        _vm.AddBuffCommand.Execute(null);
        // MemoizedCount defaults to 0 — never received a matching Char.MemSpell entry.

        await RecastAsync();

        Assert.False(CastSent("armor"));
        Assert.Contains(_vm.Toasts, t => t.Text == "Brakujące buffy nie są zapamiętane.");
    }

    [AvaloniaFact]
    public async Task RecastMissing_CastsBuffThatIsMemorized()
    {
        _vm.NewBuffName = "armor";
        _vm.AddBuffCommand.Execute(null);
        _vm.RequiredBuffs.Single().MemoizedCount = 1;

        await RecastAsync();

        Assert.True(CastSent("armor"));
    }

    // ====================================================================
    // 2. "mass " prefix matching against Char.Affects
    // ====================================================================

    [AvaloniaFact]
    public void MassPrefixedBuff_BecomesActive_WhenServerReportsPlainAffectName()
    {
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);

        SendAffects("""[{ "name": "aid", "desc": "d", "negative": false, "ending": false, "extraValue": null }]""");

        Assert.True(_vm.RequiredBuffs.Single().IsActive);
    }

    [AvaloniaFact]
    public void BaseAndMassBuff_BothBecomeActive_FromSinglePlainAffect()
    {
        _vm.NewBuffName = "aid";
        _vm.AddBuffCommand.Execute(null);
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);

        SendAffects("""[{ "name": "aid", "desc": "d", "negative": false, "ending": false, "extraValue": null }]""");

        Assert.All(_vm.RequiredBuffs, b => Assert.True(b.IsActive));
    }

    [AvaloniaFact]
    public void MassPrefixedBuff_NotActive_WhenAffectAbsent()
    {
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);

        SendAffects("""[{ "name": "bull strength", "desc": "d", "negative": false, "ending": false, "extraValue": null }]""");

        Assert.False(_vm.RequiredBuffs.Single().IsActive);
    }

    // ====================================================================
    // 3. Group-size-aware pair collapsing
    // ====================================================================

    [AvaloniaFact]
    public async Task RecastMissing_Pair_Solo_CastsBaseVersion()
    {
        _vm.NewBuffName = "aid";
        _vm.AddBuffCommand.Execute(null);
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);
        foreach (var buff in _vm.RequiredBuffs)
        {
            buff.MemoizedCount = 1;
        }

        // No Char.Group received yet — solo by default.
        await RecastAsync();

        Assert.True(CastSent("aid"));
        Assert.False(CastSent("mass aid"));
    }

    [AvaloniaFact]
    public async Task RecastMissing_Pair_WithOtherPlayer_CastsMassVersion()
    {
        _vm.NewBuffName = "aid";
        _vm.AddBuffCommand.Execute(null);
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);
        foreach (var buff in _vm.RequiredBuffs)
        {
            buff.MemoizedCount = 1;
        }

        SendGroup("""
            {
              "leader": "Agron",
              "members": [
                { "name": "Agron", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczony", "mem": 6, "is_npc": false, "room": 514 },
                { "name": "Norga", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczona", "mem": 14, "is_npc": false, "room": 514 }
              ]
            }
            """);

        await RecastAsync();

        Assert.True(CastSent("mass aid"));
        Assert.False(CastSent("aid"));
    }

    [AvaloniaFact]
    public async Task RecastMissing_Pair_OnlyNpcCompanion_StillCastsBaseVersion()
    {
        _vm.NewBuffName = "aid";
        _vm.AddBuffCommand.Execute(null);
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);
        foreach (var buff in _vm.RequiredBuffs)
        {
            buff.MemoizedCount = 1;
        }

        SendGroup("""
            {
              "leader": "Agron",
              "members": [
                { "name": "Agron", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczony", "mem": 6, "is_npc": false, "room": 514 },
                { "name": "Wilk", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczony", "mem": 0, "is_npc": true, "room": 514 }
              ]
            }
            """);

        await RecastAsync();

        Assert.True(CastSent("aid"));
        Assert.False(CastSent("mass aid"));
    }

    [AvaloniaFact]
    public async Task RecastMissing_Pair_PreferredVariantUnmemorized_FallsBackToOtherVariant()
    {
        _vm.NewBuffName = "aid";
        _vm.AddBuffCommand.Execute(null);
        _vm.NewBuffName = "mass aid";
        _vm.AddBuffCommand.Execute(null);
        // Solo (prefers base "aid"), but only "mass aid" is actually memorized.
        _vm.RequiredBuffs.Single(b => b.Name == "mass aid").MemoizedCount = 1;

        await RecastAsync();

        Assert.True(CastSent("mass aid"));
        Assert.False(CastSent("aid"));
    }

    [AvaloniaFact]
    public async Task RecastMissing_UnpairedBuff_CastRegardlessOfGroupSize()
    {
        _vm.NewBuffName = "armor";
        _vm.AddBuffCommand.Execute(null);
        _vm.RequiredBuffs.Single().MemoizedCount = 1;

        SendGroup("""
            {
              "leader": "Agron",
              "members": [
                { "name": "Agron", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczony", "mem": 6, "is_npc": false, "room": 514 },
                { "name": "Norga", "pos": "stoi", "hp": "żadnych śladów", "mv": "lekko zmęczona", "mem": 14, "is_npc": false, "room": 514 }
              ]
            }
            """);

        await RecastAsync();

        Assert.True(CastSent("armor"));
    }
}
