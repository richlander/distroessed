using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
/// <summary>
/// Detailed index for a specific patch release (e.g., 9.0.0).
/// For CVE details, follow the month or cve-json links to the timeline.
/// </summary>
[Description("Detailed index for a specific patch release. For CVE details, use month or cve-json link.")]
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
    [property: Description("True if this release includes security fixes (CVEs)")]
    bool Security,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers fixed in this release (for quick enumeration)")]
    IReadOnlyList<string>? CveRecords)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the patch release")]
    public string? Description { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the previous patch")]
    public DateTimeOffset? PrevPatchDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the previous security patch")]
    public DateTimeOffset? PrevSecurityPatchDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Highest SDK version shipped with this runtime patch")]
    public string? SdkVersion { get; init; }

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
/// Embedded content for patch detail index.
/// CVE disclosures are in the timeline month index, not here.
/// </summary>
[Description("Container for embedded runtime and SDK releases. For CVE disclosures, use the cve-json link.")]
public record PatchDetailIndexEmbedded
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Runtime release with release notes")]
    public RuntimeEntry? Runtime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Highest SDK release as a feature band object (for quick lookup)")]
    public SdkFeatureBandEntry? Sdk { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("All SDK feature bands shipped with this runtime patch")]
    public IReadOnlyList<SdkFeatureBandEntry>? SdkFeatureBands { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Component-specific documentation (e.g., aspnetcore, efcore)")]
    public Dictionary<string, HalLink>? Documentation { get; set; }
}

/// <summary>
/// Runtime entry containing version and release notes links.
/// </summary>
[Description("Runtime release entry with release notes")]
public record RuntimeEntry(
    [property: Description("Runtime version (same as patch version, e.g., '9.0.1')")]
    string Version,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for runtime release notes")]
    Dictionary<string, HalLink> Links);
