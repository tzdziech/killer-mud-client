using MudClient.Core.Automation;
using MudClient.Core.Gmcp;

namespace MudClient.Core.Tests;

public sealed class LeaderSnapPolicyTests
{
    [Fact]
    public void IsLeaderSnap_LeaderSnapsFingers_ReturnsTrue()
    {
        var group = Group(Member("Arevius", isLeader: true));

        Assert.True(LeaderSnapPolicy.IsLeaderSnap("Arevius pstryka palcami.", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_LeaderSnapsFingers_WithoutTrailingPeriod_ReturnsTrue()
    {
        var group = Group(Member("Arevius", isLeader: true));

        Assert.True(LeaderSnapPolicy.IsLeaderSnap("Arevius pstryka palcami", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_IsCaseInsensitiveWhenCheckingLeaderName()
    {
        var group = Group(Member("AREVIUS", isLeader: true));

        Assert.True(LeaderSnapPolicy.IsLeaderSnap("Arevius pstryka palcami.", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_NonLeaderMemberSnaps_ReturnsFalse()
    {
        var group = Group(Member("Arevius", isLeader: false));

        Assert.False(LeaderSnapPolicy.IsLeaderSnap("Arevius pstryka palcami.", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_UnknownPersonSnaps_ReturnsFalse()
    {
        var group = Group(Member("Arevius", isLeader: true));

        Assert.False(LeaderSnapPolicy.IsLeaderSnap("Obcy pstryka palcami.", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_SelfIsLeaderAndSnaps_ReturnsFalse()
    {
        var group = Group(Member("Ja", isLeader: true));

        Assert.False(LeaderSnapPolicy.IsLeaderSnap("Ja pstryka palcami.", "Ja", group));
    }

    [Fact]
    public void IsLeaderSnap_WithoutGroupState_ReturnsFalse()
    {
        Assert.False(LeaderSnapPolicy.IsLeaderSnap("Arevius pstryka palcami.", "Ja", null));
    }

    [Theory]
    [InlineData("Arevius pstryka w palce.")]
    [InlineData("Arevius klaszcze.")]
    [InlineData(" Arevius pstryka palcami.")]
    public void IsLeaderSnap_LineDoesNotMatchEmote_ReturnsFalse(string line)
    {
        var group = Group(Member("Arevius", isLeader: true));

        Assert.False(LeaderSnapPolicy.IsLeaderSnap(line, "Ja", group));
    }

    private static CharacterGroupUpdate Group(params CharacterGroupMember[] members) =>
        new(null, members);

    private static CharacterGroupMember Member(string name, bool isLeader) =>
        new(name, "standing", "", null, "", null, null, false, "100", isLeader);
}
