using MudClient.App.Models;
using MudClient.Core.Map;

namespace MudClient.App.ViewModels;

/// <summary>
/// One or more Killeropedia teachers whose known room resolves to the same map room — grouped so
/// WorldMapControl's hover tooltip can list everyone the player would meet by walking there.
/// </summary>
public sealed record TeacherMapMarker(MapRoom Room, IReadOnlyList<TeacherEntry> Teachers);
