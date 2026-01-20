using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
/// <summary>
/// Provides an index of patch .NET releases within a major version (e.g., 8.0.1, 8.0.2).
/// Uses simplified lifecycle information with only phase and release-date.
/// </summary>
[Description("Index of patch .NET releases with simplified lifecycle information")]
public record PatchReleaseVersionIndex(
    [property: Description("Type of release document, always 'index' for version-based indexes")]
    ReleaseKind Kind,
    [property: Description("Concise title for the document")]
    string Title) : IReleaseVersionIndex
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the index scope")]
    public string? Description { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Target framework moniker for this version (e.g., 'net10.0', 'netcoreapp3.1')")]
    public string? TargetFramework { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest patch version (e.g., '9.0.11')")]
    public string? LatestPatch { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest patch")]
    public DateTimeOffset? LatestPatchDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest patch version with security fixes (e.g., '9.0.10')")]
    public string? LatestSecurityPatch { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest security patch")]
    public DateTimeOffset? LatestSecurityPatchDate { get; init; }

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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Usage information and term definitions")]
    public UsageWithLinks? Usage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Glossary of terms and definitions")]
    public Dictionary<string, string>? Glossary { get; set; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded patch release entries, timeline years, and CVE records")]
    public PatchReleaseVersionIndexEmbedded? Embedded { get; set; }
}

[Description("Container for embedded patch release entries in a patch release index")]
public record PatchReleaseVersionIndexEmbedded(
    [Description("List of patch release entries - use 'release' property to filter by major version")]
    List<PatchReleaseVersionIndexEntry> Patches)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE IDs affecting this major version")]
    public IReadOnlyList<string>? CveRecords { get; set; }

    [JsonPropertyName("sdk_feature_bands"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK feature bands for this major version (8.0+)")]
    public IReadOnlyList<SdkFeatureBandEntry>? SdkFeatureBands { get; init; }
}

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
// Phases: preview, go-live, active, maintenance, eol
/// <summary>
/// Patch release entry within a major version or month index.
/// CVE details are available via the cve-json link in the month index.
/// </summary>
[Description("Patch release entry within a major version or month index. For CVE details, use the cve-json link.")]
public record PatchReleaseVersionIndexEntry(
    [property: Description("Patch version identifier (e.g., '8.0.1', '9.0.2')")]
    string Version,
    [property: Description("Release date when this patch became generally available")]
    DateTimeOffset Date,
    [property: Description("Release year (e.g., '2025') for filtering")]
    string Year,
    [property: Description("Release month (e.g., '10') for filtering")]
    string Month,
    [property: Description("True if this release includes security fixes (CVEs)")]
    bool Security,
    [property: Description("Support phase at time of release (preview, go-live, active, maintenance, eol)")]
    SupportPhase SupportPhase)
{
    [JsonPropertyName("major_release"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Major version this patch belongs to (e.g., '9.0', '10.0') - included in month-index for filtering, omitted in major-version-index")]
    public string? MajorRelease { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Highest SDK version included in this patch release")]
    public string? SdkVersion { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this patch release's content")]
    public Dictionary<string, HalLink> Links { get; init; } = [];
}

