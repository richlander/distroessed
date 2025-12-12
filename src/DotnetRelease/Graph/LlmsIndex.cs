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
     Description("URLs to required pre-reading for optimal graph navigation (first is quick reference, second is schema reference)")]
    public IReadOnlyList<string>? RequiredPreRead { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest stable major version (e.g., '10.0')")]
    public string? Latest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest LTS major version")]
    public string? LatestLts { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest year with releases")]
    public string? LatestYear { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Supported major version identifiers (e.g., ['10.0', '9.0', '8.0'])")]
    public IReadOnlyList<string>? Releases { get; init; }

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
     Description("Security status per release for the latest security month")]
    public IReadOnlyList<LlmsSecurityStatusEntry>? LatestSecurityMonth { get; init; }
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date")]
    public DateTimeOffset? Date { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release year (e.g., '2025')")]
    public string? Year { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release month (e.g., '10')")]
    public string? Month { get; init; }

    [Description("Whether this release includes security fixes")]
    public bool Security { get; init; }

    [Description("Number of CVEs addressed (0 if not a security release)")]
    public int CveCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers (omitted when cve_count is 0)")]
    public IReadOnlyList<string>? CveRecords { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Current support phase")]
    public SupportPhase? SupportPhase { get; init; }

    [Description("Whether this release is currently supported")]
    public bool Supported { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("End of life date for this release")]
    public DateOnly? EolDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK version shipped with this runtime patch")]
    public string? SdkVersion { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links - self points to patch index")]
    public Dictionary<string, HalLink> Links { get; init; } = [];
}

/// <summary>
/// Security status entry for a release in the latest security month.
/// Answers: "What do I need to know about security for each release?"
/// </summary>
[Description("Security status entry per release for the latest security month")]
public record LlmsSecurityStatusEntry(
    [property: Description("Major version (e.g., '9.0')")]
    string Release)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release type: lts or sts")]
    public ReleaseType? ReleaseType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Patch version with security fixes")]
    public string? Version { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK version for the security patch")]
    public string? SdkVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the security patch")]
    public DateTimeOffset? Date { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Year of the security month")]
    public string? Year { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Month of the security release")]
    public string? Month { get; init; }

    [Description("Always true for security status entries")]
    public bool Security { get; init; } = true;

    [Description("Number of CVEs addressed")]
    public int CveCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers")]
    public IReadOnlyList<string>? CveRecords { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links - self points to the month index")]
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
