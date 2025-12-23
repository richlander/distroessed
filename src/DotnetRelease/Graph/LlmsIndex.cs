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
     Description("Note for human readers about this index")]
    public string? HumanNote { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("URL to required pre-reading for optimal graph navigation")]
    public string? RequiredPreRead { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest major .NET version (e.g., '10.0')")]
    public string? LatestMajor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest LTS major .NET version (e.g., '10.0')")]
    public string? LatestLtsMajor { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest patch across all supported releases")]
    public DateTimeOffset? LatestPatchDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest security patch across all supported releases")]
    public DateTimeOffset? LatestSecurityPatchDate { get; init; }

    [JsonPropertyName("supported_major_releases"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Supported major version identifiers (e.g., ['10.0', '9.0', '8.0'])")]
    public IReadOnlyList<string>? SupportedMajorReleases { get; init; }

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
     Description("Current patch for each supported release (one per entry in supported_major_releases)")]
    public IReadOnlyList<LlmsPatchEntry>? Patches { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Last 3 security months (most recent first), crossing year boundaries if needed")]
    public IReadOnlyList<HistoryMonthSummary>? LatestSecurityMonths { get; init; }
}

/// <summary>
/// Patch entry shape for LLMs index - optimized for AI consumption.
/// Includes denormalized data to minimize required fetches.
/// </summary>
[Description("Patch entry optimized for AI consumption")]
public record LlmsPatchEntry(
    [property: Description("Full patch version (e.g., '9.0.10', '10.0.1')")]
    string Version,
    [property: JsonPropertyName("major_release"), Description("Major version this patch belongs to (e.g., '9.0', '10.0')")]
    string MajorRelease,
    [property: Description("Release type: lts or sts (denormalized from major)")]
    ReleaseType ReleaseType,
    [property: Description("Whether this release includes security fixes")]
    bool Security,
    [property: Description("Current support phase")]
    SupportPhase SupportPhase,
    [property: Description("Whether this release is currently supported")]
    bool Supported,
    [property: Description("SDK version shipped with this runtime patch")]
    string SdkVersion,
    [property: Description("Latest security patch version for this release")]
    string LatestSecurityPatch,
    [property: Description("Release date of the latest security patch")]
    DateTimeOffset LatestSecurityPatchDate,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links - self points to patch index")]
    Dictionary<string, HalLink> Links);

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
     Description("Note for human readers about this index")]
    public string? HumanNote { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Override title if needed")]
    public string? Title { get; init; }

    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Additional links to merge")]
    public Dictionary<string, HalLink>? Links { get; init; }
}
