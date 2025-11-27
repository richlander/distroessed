using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
/// <summary>
/// Detailed index for a specific patch release (e.g., 9.0.0) containing CVE disclosures
/// </summary>
[Description("Detailed index for a specific patch release with CVE disclosure information")]
public record PatchDetailIndex(
    [Description("Type of release document, always 'patch-index' for patch detail indexes")]
    ReleaseKind Kind,
    [Description("Patch version identifier (e.g., '8.0.1', '9.0.2')")]
    string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date when this patch became generally available")]
    DateTimeOffset? Date,
    [property: Description("True if this release includes security fixes (CVEs); defaults to true for safety")]
    bool Security,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase at time of release (preview, go-live, active, maintenance, eol)")]
    SupportPhase? SupportPhase,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the patch release")]
    string Description)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Runtime version for this patch (same as version)")]
    public string? RuntimeVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK versions shipped with this runtime patch")]
    public IReadOnlyList<string>? SdkVersions { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded runtime, SDK, and CVE disclosure information")]
    public PatchDetailIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

/// <summary>
/// Embedded content for patch detail index
/// </summary>
[Description("Container for embedded runtime, SDK versions and CVE disclosures")]
public record PatchDetailIndexEmbedded
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Runtime information with release notes links")]
    public PatchRuntimeInfo? Runtime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK versions shipped with this runtime patch")]
    public IReadOnlyList<PatchSdkEntry>? Sdks { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE IDs associated with this patch release")]
    public IReadOnlyList<string>? CveRecords { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability disclosures for this patch release")]
    public IReadOnlyList<CveRecordSummary>? Disclosures { get; set; }
}

/// <summary>
/// Runtime information for a patch release
/// </summary>
[Description("Runtime version and links for a patch release")]
public record PatchRuntimeInfo(
    [Description("Runtime version")]
    string Version)
{
    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Links to runtime release notes")]
    public Dictionary<string, HalLink>? Links { get; init; }
}

/// <summary>
/// SDK entry for a patch release
/// </summary>
[Description("SDK version and links for a patch release")]
public record PatchSdkEntry(
    [Description("SDK version")]
    string Version)
{
    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Links to SDK feature band and release notes")]
    public Dictionary<string, HalLink>? Links { get; init; }
}
