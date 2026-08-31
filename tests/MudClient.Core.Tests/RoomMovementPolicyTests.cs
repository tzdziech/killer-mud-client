using MudClient.Core.Map;

namespace MudClient.Core.Tests;

public sealed class RoomMovementPolicyTests
{
    [Fact]
    public void FindExit_MatchesShortFullAndNamedCommands()
    {
        RoomExitInfo[] exits =
        [
            new("N", null, false, false),
            new("E", "brama", true, true),
        ];

        Assert.Equal(exits[0], RoomMovementPolicy.FindExit("north", exits));
        Assert.Equal(exits[0], RoomMovementPolicy.FindExit("n", exits));
        Assert.Equal(exits[1], RoomMovementPolicy.FindExit("brama", exits));
    }

    [Fact]
    public void BuildInitialCommands_ClosedNamedDoor_OpensThenUsesFoldedName()
    {
        var exit = new RoomExitInfo("N", "żółte wyjście", true, true);

        Assert.Equal(
            ["open zolte wyjscie", "zolte wyjscie"],
            RoomMovementPolicy.BuildInitialCommands(exit));
    }

    [Fact]
    public void BuildInitialCommands_OpenOrOrdinaryExit_SendsOnlyMovement()
    {
        Assert.Equal(
            ["E"],
            RoomMovementPolicy.BuildInitialCommands(new RoomExitInfo("E", null, false, false)));
        Assert.Equal(
            ["brama"],
            RoomMovementPolicy.BuildInitialCommands(new RoomExitInfo("W", "brama", true, false)));
    }
}
