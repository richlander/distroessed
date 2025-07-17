using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Contains comprehensive metadata about a specific .NET major release, including support lifecycle information")]
public record ReleaseManifest(
    [Description("Type of release document, always 'manifest'")]
    ReleaseKind Kind,
    [property: JsonPropertyName("_links"), Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links,
    [Description("Major version identifier (e.g., '8.0')")]
    string Version,
    [Description("Human-friendly version label (e.g., '.NET 8.0')")]
    string Label,
    [Description("General Availability release date in ISO 8601 format")]
    DateTimeOffset GaDate,
    [Description("End of Life date in ISO 8601 format")]
    DateTimeOffset EolDate,
    [Description("Release support model (LTS or STS)")]
    ReleaseType ReleaseType,
    [Description("Current lifecycle phase (preview, active, maintenance, eol)")]
    SupportPhase SupportPhase);

    