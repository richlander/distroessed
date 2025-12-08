using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// Provides an index of .NET SDK releases organized by feature bands.
/// Follows the HAL+JSON specification for hypermedia navigation.
/// </summary>
[Description("Index of .NET SDK releases organized by feature bands, supporting navigation from major versions to specific SDK releases")]
public record SdkVersionIndex(
    [Description("Type of release document, always 'sdk-index' for SDK indexes")]
    ReleaseKind Kind,
    [Description("SDK major version (e.g., '8.0', '9.0')")]
    string Version,
    [Description("Concise title for the document")]
    string Title)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the SDK index")]
    public string? Description { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest SDK version")]
    public string? Latest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest SDK version that includes security fixes")]
    public string? LatestSecurity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest active feature band version (e.g., '8.0.4xx')")]
    public string? LatestFeatureBand { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink>? Links { get; init; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded SDK feature band entries and releases")]
    public SdkVersionIndexEmbedded? Embedded { get; set; }
}

[Description("Container for embedded SDK feature band entries")]
public record SdkVersionIndexEmbedded(
    [Description("List of SDK feature band entries with version information and navigation links")]
    List<SdkFeatureBandEntry> FeatureBands);

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
[Description("Individual SDK feature band entry containing version metadata and navigation links")]
public record SdkFeatureBandEntry(
    [Description("Latest SDK version in this feature band (e.g., '9.0.307')")]
    string Version,
    [Description("Feature band identifier (e.g., '9.0.3xx')")]
    string Band,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date of the latest SDK in this feature band")]
    DateTimeOffset? Date,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Descriptive label for the feature band")]
    string? Label,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase (preview, go-live, active, maintenance, eol)")]
    SupportPhase? SupportPhase,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this feature band's content")]
    Dictionary<string, HalLink> Links);

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
[Description("Individual SDK release entry containing version metadata and navigation links")]
public record SdkReleaseEntry(
    [Description("SDK version (e.g., '8.0.100', '9.0.200')")]
    string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release date when this SDK version became generally available")]
    DateTimeOffset? Date,
    [Description("Whether this release includes security fixes")]
    bool Security,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase at time of release (preview, go-live, active, maintenance, eol)")]
    SupportPhase? SupportPhase,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this SDK release's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("List of CVE IDs addressed in this release")]
    public IReadOnlyList<string>? CveRecords { get; init; }
};

// Support phases are defined in https://github.com/dotnet/core/blob/main/release-policies.md
/// <summary>
/// Represents a convenient file format for SDK download links with HAL+JSON support
/// </summary>
[Description("SDK download information with direct links to installation files")]
public record SdkDownloadInfo(
    [Description("Type of release document, always 'sdk-download' for SDK download indexes")]
    ReleaseKind Kind,
    [Description("SDK version (e.g., '8.0.1xx')")]
    string Version,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase (preview, go-live, active, maintenance, eol)")]
    SupportPhase? SupportPhase,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the SDK download")]
    string Description,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation")]
    Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded SDK downloads organized by RID for direct lookup")]
    public SdkDownloadEmbedded? Embedded { get; set; }
}

[Description("Container for SDK downloads keyed by runtime identifier")]
public record SdkDownloadEmbedded(
    [Description("Dictionary of SDK downloads keyed by RID (e.g., 'linux-x64', 'win-x64')")]
    Dictionary<string, SdkDownloadFile> Downloads);

[Description("Individual SDK download file for a specific platform")]
public record SdkDownloadFile(
    [Description("File name")]
    string Name,
    [Description("Runtime identifier")]
    string Rid,
    [Description("Operating system")]
    string Os,
    [Description("Architecture")]
    string Arch,
    [Description("Hash algorithm used for file verification (e.g., 'sha512')")]
    string HashAlgorithm,
    [property: JsonPropertyName("_links"),
     Description("Download and hash links")]
    Dictionary<string, HalLink> Links);