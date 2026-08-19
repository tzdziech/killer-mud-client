using MudClient.App.Services;
using MudClient.App.ViewModels;
using MudClient.Core.Gmcp;
using Xunit;

namespace MudClient.App.Tests;

/// <summary>Regression coverage for a bug where every single Char.Group GMCP update (which fires
/// very frequently — any member's position or HP changing, most often during combat) did a
/// Group.Clear()+Add() rebuild of every member, tearing down and recreating every row's visual
/// container on every single update. That could land between a pointer-press and
/// pointer-release on a per-member button (e.g. a group spell shortcut), silently dropping the
/// click — reported as "buttons in the Group panel keep refreshing and some clicks stop working".
/// RefreshVisibleGroup now updates in place: an unchanged member (GroupMember is a record, so
/// value-equality is free) keeps its exact same instance, so ItemsControl never touches its
/// container.</summary>
public sealed class GroupPanelRefreshTests
{
    private static MainWindowViewModel CreateViewModel(out string directory)
    {
        directory = Path.Combine(
            Path.GetTempPath(), "KillerMudClient_GroupRefreshTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return new MainWindowViewModel(settingsService: new AppSettingsService(directory));
    }

    private static CharacterGroupUpdate Update(params CharacterGroupMember[] members) =>
        new(Leader: null, Members: members);

    private static CharacterGroupMember Member(string name, string hpText = "bez ran", int? hpScale = 7) =>
        new(name, "standing", hpText, hpScale, "wypoczęty", 4, null, false, "6017", false);

    [Fact]
    public void RefreshVisibleGroup_IdenticalUpdate_KeepsSameMemberInstance()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            viewModel.RefreshVisibleGroup(Update(Member("Aragorn")));
            var before = Assert.Single(viewModel.Group);

            viewModel.RefreshVisibleGroup(Update(Member("Aragorn")));
            var after = Assert.Single(viewModel.Group);

            Assert.Same(before, after);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RefreshVisibleGroup_OneMemberChanges_OnlyThatEntryIsReplaced()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            viewModel.RefreshVisibleGroup(Update(Member("Aragorn"), Member("Legolas")));
            var aragornBefore = viewModel.Group[0];
            var legolasBefore = viewModel.Group[1];

            // Legolas takes damage; Aragorn's data is unchanged.
            viewModel.RefreshVisibleGroup(Update(
                Member("Aragorn"), Member("Legolas", hpText: "lekko draśnięty", hpScale: 6)));

            Assert.Same(aragornBefore, viewModel.Group[0]);
            Assert.NotSame(legolasBefore, viewModel.Group[1]);
            Assert.Equal("lekko draśnięty", viewModel.Group[1].HpText);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RefreshVisibleGroup_MemberLeaves_TrimsTailWithoutTouchingSurvivors()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            viewModel.RefreshVisibleGroup(Update(Member("Aragorn"), Member("Legolas")));
            var aragornBefore = viewModel.Group[0];

            viewModel.RefreshVisibleGroup(Update(Member("Aragorn")));

            var remaining = Assert.Single(viewModel.Group);
            Assert.Same(aragornBefore, remaining);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RefreshVisibleGroup_MemberJoins_AppendsWithoutTouchingExisting()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            viewModel.RefreshVisibleGroup(Update(Member("Aragorn")));
            var aragornBefore = viewModel.Group[0];

            viewModel.RefreshVisibleGroup(Update(Member("Aragorn"), Member("Legolas")));

            Assert.Equal(2, viewModel.Group.Count);
            Assert.Same(aragornBefore, viewModel.Group[0]);
            Assert.Equal("Legolas", viewModel.Group[1].Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void RefreshVisibleGroup_SelfExcludedFromList()
    {
        var viewModel = CreateViewModel(out var directory);
        try
        {
            var self = typeof(MainWindowViewModel)
                .GetField("_latestCharacterName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            self.SetValue(viewModel, "Aragorn");

            viewModel.RefreshVisibleGroup(Update(Member("Aragorn"), Member("Legolas")));

            var remaining = Assert.Single(viewModel.Group);
            Assert.Equal("Legolas", remaining.Name);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
