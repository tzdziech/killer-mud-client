namespace MudClient.Core.Map;

/// <summary>
/// Picks auto-farm's next room: the cheapest-to-reach (by <see cref="MapPathfinder"/> cost) room
/// inside a <see cref="FarmRegion"/> that hasn't been visited yet and has a resolvable vnum
/// (walking always routes by vnum, so a room without one can never be a destination).
/// </summary>
public static class FarmTraversalPlanner
{
    public static MapRoom? FindNearestUnvisitedRoom(
        MapPathfinder pathfinder,
        MapIndex index,
        FarmRegion region,
        int currentRoomId,
        IReadOnlySet<int> visitedRoomIds)
    {
        MapRoom? best = null;
        var bestCost = double.MaxValue;

        foreach (var candidate in RoomsInRegion(index, region))
        {
            if (candidate.Id == currentRoomId || visitedRoomIds.Contains(candidate.Id))
            {
                continue;
            }

            var path = pathfinder.FindPath(currentRoomId, candidate.Id);
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
    public static int CountUnvisited(MapIndex index, FarmRegion region, IReadOnlySet<int> visitedRoomIds) =>
        RoomsInRegion(index, region).Count(room => !visitedRoomIds.Contains(room.Id));

    public static int CountTotal(MapIndex index, FarmRegion region) =>
        RoomsInRegion(index, region).Count();

    private static IEnumerable<MapRoom> RoomsInRegion(MapIndex index, FarmRegion region) =>
        index.GetRoomsInBounds(region.AreaId, region.Z, region.MinX, region.MinY, region.MaxX, region.MaxY)
            .Where(room => !string.IsNullOrWhiteSpace(room.Vnum));
}
