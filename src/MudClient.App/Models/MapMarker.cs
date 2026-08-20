namespace MudClient.App.Models;

/// <summary>
/// A player-placed local marker on a specific room, keyed by vnum. One marker per vnum — setting
/// a new symbol on an already-marked room replaces it. <see cref="Note"/> is a free-text note
/// attached alongside the symbol (see MapViewModel.SetNoteOnSelectedRoom) — shown on hover the
/// same way a teacher's/spellbook mob's info is, and as a small gold corner badge on the map
/// itself (see WorldMapControl.DrawNoteCornerBadge). Like the symbol, a note can be shared with
/// the community via "Zgłoś znaczniki do społeczności" (see
/// MapViewModel.ComputeMarkerReportDiff) — the player reviews the pre-filled GitHub issue before
/// submitting it, same as for symbols.
/// </summary>
public sealed record MapMarker(string Vnum, string Symbol, string? Note = null);

public sealed class MapMarkerDocument
{
    public List<MapMarker> Markers { get; set; } = [];
}

/// <summary>One entry in the fixed marker legend (see <see cref="MudClient.App.ViewModels.MapViewModel.MarkerLegend"/>).
/// Phase 1 offers no way to add symbols beyond this list.</summary>
public sealed record MarkerLegendEntry(string Symbol, string Label);

/// <summary>
/// One local marker that isn't already known in the shared/community dataset — either a brand
/// new vnum (<see cref="PreviousSymbol"/> null), or one whose symbol or note disagrees with
/// what's already shared. Computed by
/// <see cref="MudClient.App.ViewModels.MapViewModel.ComputeMarkerReportDiff"/>; this is exactly
/// (and only) what "Zgłoś wszystko" includes in its report, so an already-known, unchanged marker
/// never gets resubmitted.
/// </summary>
public sealed record MapMarkerReportEntry(string Vnum, string NewSymbol, string? PreviousSymbol, string? Note = null);
