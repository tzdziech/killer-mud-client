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
