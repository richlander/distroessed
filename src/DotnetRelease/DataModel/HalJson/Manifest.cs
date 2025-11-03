using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Contains comprehensive metadata about a specific .NET major release, including support lifecycle information")]
public record ReleaseManifest(
    [Description("Type of release document, always 'manifest'")]
    ReleaseKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links,
    [Description("Major version identifier (e.g., '8.0')")]
    string Version,
    [Description("Human-friendly version label (e.g., '.NET 8.0')")]
    string Label)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support lifecycle information including release type, phase, and dates")]
    public Lifecycle? Lifecycle { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Partial manifest data for hand-maintained release information")]
public record PartialManifest(
    [property: JsonPropertyName("release-date"),
     Description("Release date in ISO 8601 format")]
    DateTimeOffset? ReleaseDate,
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

