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
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, new HashSet<int>());

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
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, visited);

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
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, new HashSet<int>());

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_IgnoresRoomsWithoutAVnum()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, new HashSet<int>());

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_NoUnvisitedRoomsLeft_ReturnsNull()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var visited = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, visited);

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
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, new HashSet<int>());

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

        Assert.Equal(1, FarmTraversalPlanner.CountUnvisited(index, [region], visited));
    }

    [Fact]
    public void CountTotal_ExcludesVnumlessRooms()
    {
        var (index, _) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 0, 1, "200"),
            Room(3, 0, 2, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);

        Assert.Equal(2, FarmTraversalPlanner.CountTotal(index, [region]));
    }

    // ====================================================================
    // excludedRoomIds — rooms the caller flags as unsafe/unreachable to deliberately stop at
    // (e.g. marked "X"/"Zamknięte" or "#"/"Przepaść" on the map).
    // ====================================================================

    [Fact]
    public void FindNearestUnvisitedRoom_ExcludedRoom_IsSkippedInFavorOfNextCheapest()
    {
        // Two independent branches: room 2 (cheaper, but excluded) and room 3 (reachable without
        // ever touching room 2) — excluding room 2 must steer the planner to room 3 instead.
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200"),
            Room(3, 1, 0, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(
            pathfinder, index, [region], 1, new HashSet<int>(), excluded);

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
            pathfinder, index, [region], 1, new HashSet<int>(), excluded);

        Assert.Null(next);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_NoExclusionSetGiven_BehavesAsBefore()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], 1, new HashSet<int>());

        Assert.NotNull(next);
        Assert.Equal(2, next.Id);
    }

    [Fact]
    public void FindNearestUnvisitedRoom_CandidateOnlyReachableThroughExcludedRoom_IsSkipped()
    {
        // Regression: room 3 is unvisited and not itself excluded, but the only route to it
        // passes through excluded room 2 — auto-farm must not walk through a marked room just
        // because the marked room isn't the final destination.
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, "200", ("north", 3)),
            Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(
            pathfinder, index, [region], 1, new HashSet<int>(), excluded);

        Assert.Null(next);
    }

    [Fact]
    public void CountUnvisited_ExcludesFlaggedRoomsToo()
    {
        var (index, _) = Build(Room(1, 0, 0, "100"), Room(2, 0, 1, "200"), Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };

        Assert.Equal(2, FarmTraversalPlanner.CountUnvisited(index, [region], new HashSet<int>(), excluded));
    }

    [Fact]
    public void CountTotal_ExcludesFlaggedRoomsToo()
    {
        var (index, _) = Build(Room(1, 0, 0, "100"), Room(2, 0, 1, "200"), Room(3, 0, 2, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2, 3 };

        Assert.Equal(1, FarmTraversalPlanner.CountTotal(index, [region], excluded));
    }

    // ====================================================================
    // BuildVisitOrder — full-tour planning (nearest-neighbor + 2-opt/Or-opt), used by
    // MainWindowViewModel.StartAutoFarm instead of calling FindNearestUnvisitedRoom fresh at
    // every arrival.
    // ====================================================================

    private static MapRoom WeightedRoom(int id, double? weight, string? vnum, params (string Name, int Target)[] exits) =>
        new()
        {
            Id = id,
            AreaId = 1,
            Coordinates = new MapCoordinates(0, 0, 0),
            Weight = weight,
            UserData = vnum is null
                ? null
                : new Dictionary<string, JsonElement> { ["vnum"] = JsonSerializer.SerializeToElement(vnum) },
            Exits = exits.Select(e => new MapExit { ExitId = e.Target, Name = e.Name }).ToList(),
        };

    private static double TotalCost(MapPathfinder pathfinder, int startId, IEnumerable<int> orderedIds)
    {
        var total = 0.0;
        var current = startId;
        foreach (var id in orderedIds)
        {
            total += pathfinder.FindPath(current, id)!.TotalCost;
            current = id;
        }

        return total;
    }

    [Fact]
    public void BuildVisitOrder_VisitsEveryCandidateExactlyOnce()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200", ("south", 1), ("east", 4)),
            Room(3, 1, 0, "300", ("west", 1)),
            Room(4, 1, 1, "400", ("west", 2)));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1);

        Assert.Equal(new[] { 2, 3, 4 }, order.Select(r => r.Id).OrderBy(id => id));
    }

    [Fact]
    public void BuildVisitOrder_IgnoresRoomsOutsideTheRegion()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 100, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1);

        Assert.Empty(order);
    }

    [Fact]
    public void BuildVisitOrder_IgnoresRoomsWithoutAVnum()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, vnum: null));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1);

        Assert.Empty(order);
    }

    [Fact]
    public void BuildVisitOrder_ExcludedRoom_IsNeverACandidateOrTransit()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200"),
            Room(3, 1, 0, "300"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var excluded = new HashSet<int> { 2 };
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], 1, excluded);

        Assert.Equal([3], order.Select(r => r.Id));
    }

    [Fact]
    public void BuildVisitOrder_UnreachableCandidate_IsOmittedRatherThanBlockingTheRest()
    {
        // Room 3 is in-region but has no path from room 1 (directed exit only goes the other way).
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2)),
            Room(2, 0, 1, "200"),
            Room(3, 0, 2, "300", ("south", 2)));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1);

        Assert.Equal([2], order.Select(r => r.Id));
    }

    [Fact]
    public void BuildVisitOrder_ZeroOrOneCandidate_ReturnsThemDirectlyWithoutOptimizing()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var region = new FarmRegion(1, 0, -10, -10, 10, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1);

        Assert.Equal([2], order.Select(r => r.Id));
    }

    [Fact]
    public void BuildVisitOrder_BranchingCorridors_CostsLessThanPureGreedyNearestNeighbor()
    {
        // A start room with two side corridors ("arms") of three rooms each. Room weights are
        // chosen so plain nearest-neighbor (what ContinueAutoFarm used to call fresh at every
        // arrival) greedily grabs the nearest room of the RIGHT arm, then finds the LEFT arm's
        // entrance nearer than continuing down the right arm — so it fully drains the left arm
        // before finally backtracking through the start room to finish the right arm. Visiting
        // each arm to completion before switching avoids that backtrack entirely.
        var rooms = new[]
        {
            WeightedRoom(1, weight: null, vnum: "100", ("r1", 2), ("l1", 5)), // Start
            WeightedRoom(2, weight: 1, vnum: "200", ("s", 1), ("r2", 3)),     // Right arm room 1
            WeightedRoom(3, weight: 3, vnum: "300", ("r1", 2), ("r3", 4)),    // Right arm room 2
            WeightedRoom(4, weight: 3, vnum: "400", ("r2", 3)),               // Right arm room 3
            WeightedRoom(5, weight: 1.5, vnum: "500", ("s", 1), ("l2", 6)),   // Left arm room 1
            WeightedRoom(6, weight: 3, vnum: "600", ("l1", 5), ("l3", 7)),    // Left arm room 2
            WeightedRoom(7, weight: 3, vnum: "700", ("l2", 6)),               // Left arm room 3
        };

        var (index, pathfinder) = Build(rooms);
        var region = new FarmRegion(1, 0, -10, -10, 10, 10);

        var planned = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [region], startRoomId: 1)
            .Select(r => r.Id).ToList();

        var greedy = new List<int>();
        var visited = new HashSet<int> { 1 };
        var current = 1;
        while (visited.Count < rooms.Length)
        {
            var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [region], current, visited);
            if (next is null)
            {
                break;
            }

            greedy.Add(next.Id);
            visited.Add(next.Id);
            current = next.Id;
        }

        var plannedCost = TotalCost(pathfinder, startId: 1, planned);
        var greedyCost = TotalCost(pathfinder, startId: 1, greedy);

        Assert.Equal(new[] { 2, 3, 4, 5, 6, 7 }, planned.OrderBy(id => id));
        Assert.True(
            plannedCost < greedyCost,
            $"Expected the planned tour ({plannedCost}) to beat pure nearest-neighbor ({greedyCost}).");
    }

    // ====================================================================
    // Multiple regions (discussion #32: "możliwość zaznaczania wielu obszarów") — every method
    // now takes a list of regions and operates on their union.
    // ====================================================================

    [Fact]
    public void BuildVisitOrder_UnionsCandidatesFromEveryRegion()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200", ("south", 1)),   // inside region A's bounds only
            Room(3, 100, 0, "300", ("west", 1))); // inside region B's bounds only

        var regionA = new FarmRegion(1, 0, -10, -10, 10, 10);
        var regionB = new FarmRegion(1, 0, 90, -10, 110, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [regionA, regionB], startRoomId: 1);

        Assert.Equal(new[] { 2, 3 }, order.Select(r => r.Id).OrderBy(id => id));
    }

    [Fact]
    public void BuildVisitOrder_RoomOutsideEveryRegion_IsIgnored()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200", ("south", 1)),      // inside region A
            Room(3, 1000, 0, "300", ("west", 1)));   // outside both regions

        var regionA = new FarmRegion(1, 0, -10, -10, 10, 10);
        var regionB = new FarmRegion(1, 0, 90, -10, 110, 10);
        var order = FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [regionA, regionB], startRoomId: 1);

        Assert.Equal([2], order.Select(r => r.Id));
    }

    [Fact]
    public void CountTotal_OverlappingRegions_DoesNotDoubleCountSharedRooms()
    {
        var (index, _) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 5, 5, "200")); // inside both overlapping regions' bounds

        var regionA = new FarmRegion(1, 0, -10, -10, 10, 10);
        var regionB = new FarmRegion(1, 0, 0, 0, 20, 20);

        Assert.Equal(2, FarmTraversalPlanner.CountTotal(index, [regionA, regionB]));
    }

    [Fact]
    public void FindNearestUnvisitedRoom_FindsCandidateInASecondRegionWhenFirstHasNone()
    {
        var (index, pathfinder) = Build(
            Room(1, 0, 0, "100", ("east", 2)),
            Room(2, 100, 0, "200", ("west", 1))); // only inside region B's bounds

        var regionA = new FarmRegion(1, 0, -10, -10, 10, 10); // empty besides the start room
        var regionB = new FarmRegion(1, 0, 90, -10, 110, 10);
        var next = FarmTraversalPlanner.FindNearestUnvisitedRoom(
            pathfinder, index, [regionA, regionB], 1, new HashSet<int>());

        Assert.Equal(2, next?.Id);
    }

    [Fact]
    public void NoRegions_EveryMethodBehavesAsAnEmptyRegion()
    {
        var (index, pathfinder) = Build(Room(1, 0, 0, "100", ("east", 2)), Room(2, 1, 0, "200"));

        Assert.Empty(FarmTraversalPlanner.BuildVisitOrder(pathfinder, index, [], startRoomId: 1));
        Assert.Null(FarmTraversalPlanner.FindNearestUnvisitedRoom(pathfinder, index, [], 1, new HashSet<int>()));
        Assert.Equal(0, FarmTraversalPlanner.CountTotal(index, []));
    }

    // ====================================================================
    // MapPathfinder.ComputeDistances — the single-source-multiple-targets primitive
    // BuildVisitOrder uses to build its distance matrix.
    // ====================================================================

    [Fact]
    public void ComputeDistances_MatchesFindPathForEveryTarget()
    {
        var (_, pathfinder) = Build(
            Room(1, 0, 0, "100", ("north", 2), ("east", 3)),
            Room(2, 0, 1, "200", ("east", 4)),
            Room(3, 1, 0, "300"),
            Room(4, 1, 1, "400"));

        var distances = pathfinder.ComputeDistances(1, [2, 3, 4]);

        Assert.Equal(pathfinder.FindPath(1, 2)!.TotalCost, distances[2]);
        Assert.Equal(pathfinder.FindPath(1, 3)!.TotalCost, distances[3]);
        Assert.Equal(pathfinder.FindPath(1, 4)!.TotalCost, distances[4]);
    }

    [Fact]
    public void ComputeDistances_UnreachableTarget_IsOmitted()
    {
        var (_, pathfinder) = Build(
            Room(1, 0, 0, "100"),
            Room(2, 0, 1, "200", ("south", 1)));

        var distances = pathfinder.ComputeDistances(1, [2]);

        Assert.Empty(distances);
    }

    [Fact]
    public void ComputeDistances_ExcludedRoom_IsBlockedEvenAsATarget()
    {
        var (_, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var distances = pathfinder.ComputeDistances(1, [2], new HashSet<int> { 2 });

        Assert.Empty(distances);
    }

    [Fact]
    public void ComputeDistances_SourceEqualsTarget_IsZero()
    {
        var (_, pathfinder) = Build(Room(1, 0, 0, "100", ("north", 2)), Room(2, 0, 1, "200"));

        var distances = pathfinder.ComputeDistances(1, [1, 2]);

        Assert.Equal(0, distances[1]);
    }
}
