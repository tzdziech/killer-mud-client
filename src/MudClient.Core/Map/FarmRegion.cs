namespace MudClient.Core.Map;

/// <summary>A rectangular region of one map area+Z that auto-farm is allowed to roam within,
/// drawn by the user on the map (world coordinates, inclusive bounds on both axes).</summary>
public readonly record struct FarmRegion(int AreaId, double Z, double MinX, double MinY, double MaxX, double MaxY)
{
    public bool Contains(MapCoordinates coordinates) =>
        coordinates.X >= MinX && coordinates.X <= MaxX &&
        coordinates.Y >= MinY && coordinates.Y <= MaxY;
}
