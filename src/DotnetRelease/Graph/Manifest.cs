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
    public SupportPhase? SupportPhase { get; init; }

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

[Description("Partial manifest data for hand-maintained release information - schema compatible with ReleaseManifest")]
public record PartialManifest
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Type of release document, always 'manifest'")]
    public ReleaseKind? Kind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Concise title for the document")]
    public string? Title { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Major version identifier (e.g., '8.0')")]
    public string? Version { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Human-friendly version label (e.g., '.NET 8.0')")]
    public string? Label { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release type: lts (Long-Term Support) or sts (Standard-Term Support)")]
    public ReleaseType? ReleaseType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Current support phase (preview, go-live, active, maintenance, eol)")]
    public SupportPhase? SupportPhase { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Whether this version is currently supported (false = compute at generation time)")]
    public bool? Supported { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("General Availability date when this version was released")]
    public DateTimeOffset? GaDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("End of Life date when support ends")]
    public DateTimeOffset? EolDate { get; init; }

    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("HAL+JSON links - self link path will be expanded with url-root at generation time")]
    public Dictionary<string, HalLink>? Links { get; init; }
}

