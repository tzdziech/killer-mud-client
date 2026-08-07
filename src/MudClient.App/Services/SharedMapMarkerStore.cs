using System.Reflection;
using System.Text.Json;
using MudClient.App.Models;

namespace MudClient.App.Services;

/// <summary>
/// The community's currently-accepted map markers — a read-only reference the client compares
/// the player's own local markers (see <see cref="MapMarkerStore"/>) against, so "Zgłoś
/// znaczniki" only reports what isn't already known. Ships bundled with the app; there is no
/// live download channel for it yet (see <see cref="RareCatalogStore"/> for what that would look
/// like once this data is actually distributed through the content-update pipeline).
/// </summary>
public sealed class SharedMapMarkerStore
{
    private const string EmbeddedResourceName = "MudClient.App.Assets.Data.map-markers-shared.json";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public MapMarkerDocument Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedResourceName)
            ?? throw new InvalidOperationException($"Brak osadzonego katalogu znaczników mapy: {EmbeddedResourceName}.");
        try
        {
            return JsonSerializer.Deserialize<MapMarkerDocument>(stream, SerializerOptions)
                ?? new MapMarkerDocument();
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Katalog wspólnych znaczników mapy ma nieprawidłowy format JSON.", exception);
        }
    }
}
