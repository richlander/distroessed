using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// Provides an index of .NET SDK releases organized by feature bands.
/// Follows the HAL+JSON specification for hypermedia navigation.
/// </summary>
[Description("Index of .NET SDK releases organized by feature bands, supporting navigation from major versions to specific SDK releases")]
public record SdkVersionIndex(
    [Description("Type of release document, always 'index' for SDK indexes")]
    ReleaseKind Kind,
    [Description("Component type, always 'sdk' for SDK indexes")]
    string Component,
    [Description("SDK version (e.g., '8.0' for .NET SDK 8.0)")]
    string Version,
    [Description("Descriptive label for the SDK version")]
    string Label,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded SDK feature band entries")]
    public SdkVersionIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded SDK feature band entries and patch releases")]
public record SdkVersionIndexEmbedded(
    [property: JsonPropertyName("latest-downloads"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest SDK downloads keyed by RID for direct access")]
    Dictionary<string, SdkDownloadFile>? LatestDownloads,
    [property: JsonPropertyName("sdk-feature-bands"),
     Description("List of SDK feature band entries with version information and navigation links")]
    List<SdkFeatureBandEntry> SdkFeatureBands,
    [property: JsonPropertyName("releases"),
     Description("List of SDK patch release entries with version information and navigation links")]
    List<ReleaseVersionIndexEntry> Releases);

[Description("Individual SDK feature band entry containing version metadata and navigation links")]
public record SdkFeatureBandEntry(
    [Description("Type of release, always 'band' for feature bands")]
    ReleaseKind Kind,
    [Description("Feature band version (e.g., '8.0.1xx', '8.0.4xx')")]
    string Version,
    [Description("Descriptive label for the feature band")]
    string Label,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this feature band's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Lifecycle information for this feature band")]
    public PatchLifecycle? Lifecycle { get; set; }
}

/// <summary>
/// Represents a convenient file format for SDK download links with HAL+JSON support
/// </summary>
[Description("SDK download information with direct links to installation files")]
public record SdkDownloadInfo(
    [Description("Component type, always 'sdk'")]
    string Component,
    [Description("SDK version (e.g., '8.0')")]
    string Version,
    [Description("Descriptive label")]
    string Label,
    [property: JsonPropertyName("hash-algorithm"),
     Description("Hash algorithm used for file verification")]
    string HashAlgorithm,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation")]
    Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded SDK downloads organized by RID for direct lookup")]
    public SdkDownloadEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Legacy array format for backward compatibility")]
    public List<SdkDownloadFile>? Files { get; set; }
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
    [property: JsonPropertyName("_links"),
     Description("Download and hash links")]
    Dictionary<string, HalLink> Links);