using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseManifest
{
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();

    public string Version { get; init; } = default!;

    public string Branding { get; init; } = default!;

    public DateOnly GaDate { get; init; }

    public DateOnly EolDate { get; init; }

    public ReleaseType ReleaseType { get; init; }

    public SupportPhase SupportPhase { get; init; }


}
