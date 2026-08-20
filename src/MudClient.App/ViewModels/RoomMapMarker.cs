using MudClient.Core.Map;

namespace MudClient.App.ViewModels;

/// <summary>A player-placed local marker (see <see cref="MudClient.App.Models.MapMarker"/>)
/// resolved to the <see cref="MapRoom"/> its vnum points at. <see cref="Note"/> is only ever
/// non-null for an explicit player marker — the auto teacher/spellbook-mob/shared-catalog layers
/// never carry one.</summary>
public sealed record RoomMapMarker(MapRoom Room, string Symbol, string? Note = null);
