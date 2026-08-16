namespace MudClient.App.Models;

/// <summary>"try &lt;n&gt;" captured live from the game — see
/// <see cref="Services.ArtifactTryMappingCoordinator"/> (the "/mapuj &lt;liczba&gt;" command) and
/// <see cref="Services.ArtifactTryStore"/>. The game's exact field layout for this response isn't
/// known ahead of time, so — like <see cref="RareEntry.Details"/> — it's kept verbatim rather than
/// parsed into structured properties.</summary>
public sealed class ArtifactTryEntry
{
    public int Number { get; set; }

    public string RawText { get; set; } = string.Empty;

    public DateTimeOffset CapturedAt { get; set; }
}

public sealed class ArtifactTryDocument
{
    public List<ArtifactTryEntry> Entries { get; set; } = [];
}
