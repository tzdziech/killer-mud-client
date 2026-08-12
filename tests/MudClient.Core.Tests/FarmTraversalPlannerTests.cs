using System.Text.Json;
using MudClient.Core.Map;

namespace MudClient.Core.Tests;

public sealed class FarmTraversalPlannerTests
{
    private static MapRoom Room(
        int id, double x, double y, string? vnum, params (string Name, int Target)[] exits) =>
        new()
        {
            Id = id,
            AreaId = 1,
            Coordinates = new MapCoordinates(x, y, 0),
            UserData = vnum is null
                ? null
                : new Dictionary<string, JsonElement>
                {
                    ["vnum"] = JsonSerializer.SerializeToElement(vnum),
                },
            Exits = exits.Select(e => new MapExit { ExitId = e.Target, Name = e.Name }).ToList(),
        };

    private static (MapIndex Index, MapPathfinder Pathfinder) Build(params MapRoom[] rooms)
    {
        var document = new MapDocument { Areas = [new MapArea { Id = 1, Rooms = rooms }] };
        var index = new MapIndex(document);
        return (index, new MapPathfinder(index));
    }

    [Fact]
    public void FindNearestUnvisitedRoom_PicksTheCheapestReachableCandidateInRegion()
    {
        // 1 -> 2 (1 hop) and 1 -> 3 (2 hops); both unvisited and in-region — 2 must win.
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 4)),
            Room(2, 0, 1, "200"),
            Room(4, 1, 0, "400", ("north", 3)),
            Room(3, 1, 1, "300"));

        var region = new FarmRegion(AreaId: 1, Z: 0, MinX: -10, MinY: -10, MaxX: 10, MaxY: 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, new HashSet<int>());

        Assert.NotNull(next);
        Assert.Equal(2, next.Id);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_SkipsAlreadyVisitedRooms()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, "200", ("north", 3)),
            Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var visited = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, visited);

        Assert.NotNull(next);
        Assert.Equal(3, next.Id);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_IgnoresRoomsOutsideTheRegion()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 100, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, new HashSet<int>());

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_IgnoresRoomsWithoutAVnum()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, new HashSet<int>());

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_NoUnvisitedRoomsLeft_ReturnsNull()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var visited = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, visited);

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_UnreachableCandidate_IsSkipped()
    {
        // Room 2 is in-region but has no path from room 1 (directed exits only go the other way).
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 0, 1, "200", ("south", 1)));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, new HashSet<int>());

        Assert.Null(next);
    }

    [Fact]
    public void CountUnvisited_ExcludesVisitedAndVnumlessRooms()
    {
        var (index, _) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 0, 1, "200"),
            Room(3, 0, 2, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var visited = new HashSet<int> { 1 };

        Assert.Equal(1, FarmTraversalPlanner.CountUnvisited(index, region, visited));
    }

    [Fact]
    public void CountTotal_ExcludesVnumlessRooms()
    {
        var (index, _) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 0, 1, "200"),
            Room(3, 0, 2, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);

        Assert.Equal(2, FarmTraversalPlanner.CountTotal(index, region));
    }

    // ====================================================================
    // excludedRoomIds — rooms the caller flags as unsafe/unreachable to deliberately stop at
    // (e.g. marked "X"/"Zamknięte" or "#"/"Przepaść" on the map).
    // ====================================================================

    [Fact]
    public void FindNearestUnvisitedRoom_ExcludedRoom_IsSkippedInFavorOfNextCheapest()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, "200", ("north", 3)),
            Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(
            pathfinder, index, region, 1, new HashSet<int>(), excluded);

        Assert.NotNull(next);
        Assert.Equal(3, next.Id);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_AllCandidatesExcluded_ReturnsNull()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(
            pathfinder, index, region, 1, new HashSet<int>(), excluded);

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_NoExclusionSetGiven_BehavesAsBefore()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, region, 1, new HashSet<int>());

        Assert.NotNull(next);
        Assert.Equal(2, next.Id);
    }

    [Fact]
    public void CountUnvisited_ExcludesFlaggedRoomsToo()
    {
        var (index, _) = Build(Room(1, 0, 0, "100"), Room(2, 0, 1, "200"), Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };

        Assert.Equal(2, FarmTraversalPlanner.CountUnvisited(index, region, new HashSet<int>(), excluded));
    }

    [Fact]
    public void CountTotal_ExcludesFlaggedRoomsToo()
    {
        var (index, _) = Build(Room(1, 0, 0, "100"), Room(2, 0, 1, "200"), Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2, 3 };

        Assert.Equal(1, FarmTraversalPlanner.CountTotal(index, region, excluded));
    }
}
