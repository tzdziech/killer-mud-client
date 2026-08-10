using MudClient.App.Models;
using MudClient.Core.Map;

namespace MudClient.App.ViewModels;

/// <summary>
/// One or more spellbook-dropping mobs whose known room resolves to the same map room — grouped
/// so WorldMapControl's hover tooltip can list everyone the player would meet by walking there.
/// </summary>
public sealed record SpellMobMapMarker(MapRoom Room, IReadOnlyList<SpellMobEntry> Mobs);
