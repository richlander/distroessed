using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// AI-optimized index providing quick access to latest releases and security information.
/// Designed for LLM consumption with embedded patch entries and security status.
/// </summary>
[Description("AI-optimized .NET release index with latest patches and security information")]
public record LlmsIndex(
    [property: Description("Type of release document, always 'llms-index'")]
    ReleaseKind Kind,
    [property: Description("Concise title for the document")]
    string Title)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Note for AI assistants on how to navigate this graph")]
    public string? AiNote { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("URL to required pre-reading for optimal graph navigation")]
    public string? RequiredPreRead { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest stable major version (e.g., '10.0')")]
    public string? Latest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest LTS major version")]
    public string? LatestLts { get; init; }

    [JsonPropertyName("supported_releases"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Supported major version identifiers (e.g., ['10.0', '9.0', '8.0'])")]
    public IReadOnlyList<string>? SupportedReleases { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded latest patches and security status")]
    public LlmsIndexEmbedded? Embedded { get; init; }
}

[Description("Container for embedded collections in the LLMs index")]
public record LlmsIndexEmbedded
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest patch for each supported release")]
    public IReadOnlyList<LlmsPatchEntry>? LatestPatches { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Last 3 security months (most recent first), crossing year boundaries if needed")]
    public IReadOnlyList<HistoryMonthSummary>? LatestSecurityMonths { get; init; }
}

/// <summary>
/// Patch entry shape for LLMs index - follows the unified patch entry shape from spec.
/// Includes `release` property for consistent filtering across all contexts.
/// </summary>
[Description("Patch entry with release property for AI consumption")]
public record LlmsPatchEntry(
    [property: Description("Full patch version (e.g., '9.0.10', '10.0.1')")]
    string Version,
    [property: Description("Major version this patch belongs to (e.g., '9.0', '10.0')")]
    string Release)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release type: lts or sts")]
    public ReleaseType? ReleaseType { get; init; }

    [Description("Whether this release includes security fixes")]
    public bool Security { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Current support phase")]
    public SupportPhase? SupportPhase { get; init; }

    [Description("Whether this release is currently supported")]
    public bool Supported { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK version shipped with this runtime patch")]
    public string? SdkVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest security patch version for this release")]
    public string? LatestSecurity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest security patch")]
    public DateOnly? LatestSecurityDate { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links - self points to patch index")]
    public Dictionary<string, HalLink> Links { get; init; } = [];
}

/// <summary>
/// Partial LLMs index for hand-maintained fields (like ai_note).
/// Loaded from _llms.json and merged with computed values.
/// </summary>
[Description("Partial LLMs index for hand-maintained fields")]
public record PartialLlmsIndex
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Note for AI assistants on how to navigate this graph")]
    public string? AiNote { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Override title if needed")]
    public string? Title { get; init; }

    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Additional links to merge")]
    public Dictionary<string, HalLink>? Links { get; init; }
}
