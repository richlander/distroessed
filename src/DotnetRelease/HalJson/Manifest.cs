using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseManifest(
    ReleaseKind Kind,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    string Version,
    string Label,
    DateTimeOffset GaDate,
    DateTimeOffset EolDate,
    ReleaseType ReleaseType,
    SupportPhase SupportPhase);

    