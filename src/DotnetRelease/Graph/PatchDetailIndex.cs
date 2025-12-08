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
    [Description("Concise title for the document")]
    string Title,
    [Description("Patch version identifier (e.g., '8.0.1', '9.0.2')")]
    string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date when this patch became generally available")]
    DateTimeOffset? Date,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase at time of release (preview, go-live, active, maintenance, eol)")]
    SupportPhase? SupportPhase,
    [property: Description("True if this release includes security fixes (CVEs); defaults to true for safety")]
    bool Security,
    [Description("Number of CVEs fixed in this release")]
    int CveCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers fixed in this release (for quick enumeration)")]
    IReadOnlyList<string>? CveRecords)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the patch release")]
    public string? Description { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Highest SDK version shipped with this runtime patch")]
    public string? SdkRelease { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK feature band versions shipped with this runtime patch")]
    public IReadOnlyList<string>? SdkFeatureBands { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded runtime, SDK, and CVE disclosure information")]
    public PatchDetailIndexEmbedded? Embedded { get; set; }
}

/// <summary>
/// Embedded content for patch detail index
/// </summary>
[Description("Container for embedded SDK releases and CVE disclosures")]
public record PatchDetailIndexEmbedded
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Highest SDK release as a feature band object (for quick lookup)")]
    public SdkFeatureBandEntry? SdkRelease { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("All SDK feature bands shipped with this runtime patch")]
    public IReadOnlyList<SdkFeatureBandEntry>? SdkFeatureBands { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability disclosures for this patch release")]
    public IReadOnlyList<CveRecordSummary>? Disclosures { get; set; }
}
