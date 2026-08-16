namespace MudClient.Core.Map;

/// <summary>
/// Picks auto-farm's next room: the cheapest-to-reach (by <see cref="MapPathfinder"/> cost) room
/// inside a <see cref="FarmRegion"/> that hasn't been visited yet, has a resolvable vnum (walking
/// always routes by vnum, so a room without one can never be a destination), and isn't in
/// <c>excludedRoomIds</c> (rooms the caller has flagged as unsafe, e.g. marked "X"/"Zamknięte",
/// "#"/"Przepaść", "!"/"!!" on the map — see MapViewModel.AutoFarmAvoidedMarkerSymbols in
/// MudClient.App). <c>excludedRoomIds</c> is also forwarded into <see cref="MapPathfinder.FindPath"/>
/// so a marked room is skipped as transit too, not just ruled out as a destination — otherwise the
/// cheapest route to a perfectly safe room could still walk straight through a flagged one.
/// </summary>
public static class FarmTraversalPlanner
{
    /// <summary>Safety cap on 2-opt passes in <see cref="BuildVisitOrder"/> — each pass is
    /// O(n²), and real tours converge (no more improving swap found) within a handful of passes,
    /// so this only bounds pathological worst cases on very large regions.</summary>
    private const int MaxImprovementPasses = 40;

    /// <summary>
    /// Plans a full visiting order for every unvisited, non-excluded, vnum-resolvable room in
    /// <paramref name="region"/>, starting from <paramref name="startRoomId"/> — unlike
    /// <see cref="FindNearestUnvisitedRoom"/>'s pure greedy nearest-neighbor choice made fresh at
    /// each arrival (which tends to strand far-off rooms for a chaotic, expensive detour near the
    /// end of the run), this builds the whole tour once via nearest-neighbor construction and then
    /// improves it with 2-opt (repeatedly uncrossing/reordering pairs of legs when doing so
    /// shortens the total walk) — meaningfully cutting total movement points burned per farm run.
    /// Costs come from <see cref="MapPathfinder.ComputeDistances"/> (one Dijkstra run per
    /// candidate room, not one per pair), so this is the same order of pathfinding work
    /// <see cref="FindNearestUnvisitedRoom"/> already did across a whole run, just spent upfront.
    /// </summary>
    public static IReadOnlyList<MapRoom> BuildVisitOrder(
        MapPathfinder pathfinder,
        MapIndex index,
        FarmRegion region,
        int startRoomId,
        IReadOnlySet<int>? excludedRoomIds = null)
    {
        var candidates = RoomsInRegion(index, region)
            .Where(room => room.Id != startRoomId && !(excludedRoomIds?.Contains(room.Id) ?? false))
            .ToList();

        if (candidates.Count <= 1)
        {
            return candidates;
        }

        var candidateIds = candidates.Select(room => room.Id).ToList();
        var costs = new Dictionary<int, IReadOnlyDictionary<int, double>>(candidates.Count + 1)
        {
            [startRoomId] = pathfinder.ComputeDistances(startRoomId, candidateIds, excludedRoomIds),
        };
        foreach (var candidate in candidates)
        {
            costs[candidate.Id] = pathfinder.ComputeDistances(candidate.Id, candidateIds, excludedRoomIds);
        }

        double Cost(int fromId, int toId) =>
            costs.TryGetValue(fromId, out var row) && row.TryGetValue(toId, out var cost)
                ? cost
                : double.PositiveInfinity;

        // --- Nearest-neighbor construction: path[0] is the (fixed) start room itself. ---
        var path = new List<int>(candidates.Count + 1) { startRoomId };
        var remaining = new HashSet<int>(candidateIds);
        var currentId = startRoomId;
        while (remaining.Count > 0)
        {
            var bestId = 0;
            var haveBest = false;
            var bestCost = double.PositiveInfinity;
            foreach (var candidateId in remaining)
            {
                var cost = Cost(currentId, candidateId);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestId = candidateId;
                    haveBest = true;
                }
            }

            if (!haveBest)
            {
                break; // Nothing left is reachable from here — same as FindNearestUnvisitedRoom
                       // silently stopping short of unreachable candidates.
            }

            path.Add(bestId);
            remaining.Remove(bestId);
            currentId = bestId;
        }

        ImproveTour(path, Cost);

        var roomsById = index.RoomsById;
        return path.Skip(1).Select(id => roomsById[id]).ToList();
    }

    /// <summary>
    /// Local search over the nearest-neighbor tour, alternating 2-opt and Or-opt passes until
    /// neither finds an improving move (or <see cref="MaxImprovementPasses"/> total passes are spent) —
    /// 2-opt alone only ever reverses a segment, which can't fix a tour where nearest-neighbor
    /// picked a lone nearby room, wandered off down one branch entirely, and left that branch's
    /// siblings stranded for an expensive return trip; relocating a room (Or-opt) is what actually
    /// moves it next to where it belongs.
    /// </summary>
    private static void ImproveTour(List<int> path, Func<int, int, double> cost)
    {
        for (var pass = 0; pass < MaxImprovementPasses; pass++)
        {
            var improvedBy2Opt = TryImprove2OptPass(path, cost);
            var improvedByRelocation = TryRelocateOnePass(path, cost);
            if (!improvedBy2Opt && !improvedByRelocation)
            {
                break;
            }
        }
    }

    /// <summary>
    /// One pass of classic 2-opt over an open path (no return trip to <c>path[0]</c> required):
    /// picks two non-adjacent legs (a→b) and (c→d), and if reconnecting them as (a→c) and (b→d)
    /// — which means reversing the b..c segment in between — shortens the total path, keeps the
    /// swap. Returns whether any swap was made.
    /// </summary>
    private static bool TryImprove2OptPass(List<int> path, Func<int, int, double> cost)
    {
        var n = path.Count;
        if (n < 4)
        {
            return false; // Need at least two non-adjacent legs to swap.
        }

        var improved = false;

        for (var i = 0; i < n - 2; i++)
        {
            var a = path[i];
            var b = path[i + 1];

            for (var j = i + 2; j < n - 1; j++)
            {
                var c = path[j];
                var d = path[j + 1];

                var before = cost(a, b) + cost(c, d);
                var after = cost(a, c) + cost(b, d);
                if (after < before - 1e-9)
                {
                    path.Reverse(i + 1, j - i);
                    b = path[i + 1];
                    improved = true;
                }
            }
        }

        return improved;
    }

    /// <summary>
    /// One pass of Or-opt: for each room (never <c>path[0]</c>, the fixed start), tries relocating
    /// it to every other position in the path, keeping the cheapest relocation found if it beats
    /// leaving it where it is. Complements 2-opt (see <see cref="ImproveTour"/>) by moving a single
    /// stranded room instead of only ever reversing a contiguous block. Returns whether any move
    /// was made.
    /// </summary>
    private static bool TryRelocateOnePass(List<int> path, Func<int, int, double> cost)
    {
        var improved = false;

        for (var i = 1; i < path.Count; i++)
        {
            var node = path[i];
            var prev = path[i - 1];
            var hasNext = i + 1 < path.Count;
            var next = hasNext ? path[i + 1] : 0;

            var removalDelta = hasNext
                ? cost(prev, next) - cost(prev, node) - cost(node, next)
                : -cost(prev, node);

            var bestJ = -1;
            var bestDelta = -1e-9;

            for (var j = 0; j < path.Count; j++)
            {
                if (j == i - 1 || j == i)
                {
                    continue; // Reinserting right where it already is — not a real move.
                }

                var a = path[j];
                var hasB = j + 1 < path.Count;
                var b = hasB ? path[j + 1] : 0;

                var insertionDelta = hasB
                    ? cost(a, node) + cost(node, b) - cost(a, b)
                    : cost(a, node);

                var totalDelta = removalDelta + insertionDelta;
                if (totalDelta < bestDelta)
                {
                    bestDelta = totalDelta;
                    bestJ = j;
                }
            }

            if (bestJ < 0)
            {
                continue;
            }

            path.RemoveAt(i);
            path.Insert(bestJ < i ? bestJ + 1 : bestJ, node);
            improved = true;
        }

        return improved;
    }

    public static MapRoom? FindNearestUnvisitedRoom(
        MapPathfinder pathfinder,
        MapIndex index,
        FarmRegion region,
        int currentRoomId,
        IReadOnlySet<int> visitedRoomIds,
        IReadOnlySet<int>? excludedRoomIds = null)
    {
        MapRoom? best = null;
        var bestCost = double.MaxValue;

        foreach (var candidate in RoomsInRegion(index, region))
        {
            if (candidate.Id == currentRoomId ||
                visitedRoomIds.Contains(candidate.Id) ||
                (excludedRoomIds?.Contains(candidate.Id) ?? false))
            {
                continue;
            }

            var path = pathfinder.FindPath(currentRoomId, candidate.Id, excludedRoomIds);
            if (path is null || path.TotalCost >= bestCost)
            {
                continue;
            }

            bestCost = path.TotalCost;
            best = candidate;
        }

        return best;
    }

    /// <summary>Rooms in the region still needing a visit — same filtering as
    /// <see cref="FindNearestUnvisitedRoom"/> minus the pathfinding cost check, for status text.</summary>
    public static int CountUnvisited(
        MapIndex index, FarmRegion region, IReadOnlySet<int> visitedRoomIds, IReadOnlySet<int>? excludedRoomIds = null) =>
        RoomsInRegion(index, region).Count(room =>
            !visitedRoomIds.Contains(room.Id) && !(excludedRoomIds?.Contains(room.Id) ?? false));

    public static int CountTotal(MapIndex index, FarmRegion region, IReadOnlySet<int>? excludedRoomIds = null) =>
        RoomsInRegion(index, region).Count(room => !(excludedRoomIds?.Contains(room.Id) ?? false));

    private static IEnumerable<MapRoom> RoomsInRegion(MapIndex index, FarmRegion region) =>
        index.GetRoomsInBounds(region.AreaId, region.Z, region.MinX, region.MinY, region.MaxX, region.MaxY)
            .Where(room => !string.IsNullOrWhiteSpace(room.Vnum));
}
