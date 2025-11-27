using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

[Description("Contains comprehensive metadata about a specific .NET major release, including support lifecycle information")]
public record ReleaseManifest(
    [Description("Type of release document, always 'manifest'")]
    ReleaseKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [Description("Major version identifier (e.g., '8.0')")]
    string Version,
    [Description("Human-friendly version label (e.g., '.NET 8.0')")]
    string Label)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release type: lts (Long-Term Support) or sts (Standard-Term Support)")]
    public ReleaseType? ReleaseType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Current support phase (preview, go-live, active, maintenance, eol)")]
    public SupportPhase? Phase { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Whether this version is currently supported")]
    public bool? Supported { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("General Availability date when this version was released")]
    public DateTimeOffset? GaDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("End of Life date when support ends")]
    public DateTimeOffset? EolDate { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; init; }
}

[Description("Partial manifest data for hand-maintained release information")]
public record PartialManifest(
    [property: JsonPropertyName("release-date"),
     Description("Date when the version became generally available (GA date) in ISO 8601 format")]
    DateTimeOffset? GaDate,
    [property: JsonPropertyName("eol-date"),
     Description("End of Life date in ISO 8601 format")]
    DateTimeOffset? EolDate,
    [property: JsonPropertyName("release-type"),
     Description("Release support model (LTS or STS) - overrides computed value")]
    ReleaseType? ReleaseType,
    [property: JsonPropertyName("phase"),
     Description("Current lifecycle phase - overrides computed value")]
    SupportPhase? SupportPhase)
{
    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Additional HAL+JSON links (e.g., blog posts, announcements)")]
    public Dictionary<string, HalLink>? Links { get; set; }
}

