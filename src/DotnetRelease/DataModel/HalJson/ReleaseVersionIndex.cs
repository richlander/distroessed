using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

/// <summary>
/// Provides an index of .NET releases organized by version hierarchy (major → patch releases).
/// Follows the HAL+JSON specification for hypermedia navigation.
/// </summary>
[Description("Index of .NET releases organized by version hierarchy, supporting navigation from major versions to patch releases")]
public record ReleaseVersionIndex(
    [Description("Type of release document, always 'index' for version-based indexes")]
    ReleaseKind Kind, 
    [Description("Human-readable description of the index scope")]
    string Description, 
    [property: JsonPropertyName("_links"), Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded release entries for this index level")]
    public ReleaseVersionIndexEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support lifecycle information (GA date, EOL date, release type, phase)")]
    public Support? Support { get; set; }
}

[Description("Container for embedded release entries in a version index")]
public record ReleaseVersionIndexEmbedded(
    [Description("List of release entries with version information and navigation links")]
    List<ReleaseVersionIndexEntry> Releases);

[Description("Individual release entry within a version index, containing version metadata and navigation links")]
public record ReleaseVersionIndexEntry(
    [Description("Version identifier (e.g., '8.0' for major version, '8.0.1' for patch version)")]
    string Version, 
    [Description("Type of release (index, major-release, patch-release, etc.)")]
    ReleaseKind Kind, 
    [property: JsonPropertyName("_links"), Description("HAL+JSON links for navigation to this release's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support lifecycle information for this specific release")]
    public Support? Support { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability records associated with this release")]
    public IReadOnlyList<CveRecordSummary>? CveRecords { get; set; }
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseKind>))]
[Description("Identifies the type of release or index document")]
public enum ReleaseKind
{
    [Description("Version-based index document")]
    Index,
    [Description("Release metadata document")]
    Manifest,
    [Description("Major version content")]
    MajorRelease,
    [Description("Patch version content")]
    PatchRelease,
    [Description("General content document")]
    Content,
    [Description("Unspecified type")]
    Unknown
}

