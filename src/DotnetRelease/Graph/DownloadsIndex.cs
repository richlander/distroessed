using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// Index of download files for a major .NET version.
/// Located at {version}/downloads/index.json.
/// </summary>
[Description("Index of download files for a major .NET version")]
public record DownloadsIndex(
    [Description("Type of document, always 'downloads-index'")]
    ReleaseKind Kind,
    [Description("Major version (e.g., '8.0', '9.0')")]
    string Version,
    [Description("Concise title for the document")]
    string Title)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the downloads index")]
    public string? Description { get; init; }
    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink>? Links { get; init; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded component and feature band entries")]
    public DownloadsIndexEmbedded? Embedded { get; init; }
}

[Description("Container for embedded component and feature band entries")]
public record DownloadsIndexEmbedded
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("List of component download entries (runtime, aspnetcore, windowsdesktop, sdk)")]
    public List<ComponentEntry>? Components { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("List of SDK feature band download entries")]
    public List<FeatureBandEntry>? FeatureBands { get; init; }
}

[Description("Component entry in the downloads index")]
public record ComponentEntry(
    [Description("Component name (e.g., 'runtime', 'aspnetcore', 'windowsdesktop', 'sdk')")]
    string Name,
    [Description("Human-readable title for the component")]
    string Title)
{
    [JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this component's download file")]
    public Dictionary<string, HalLink>? Links { get; init; }
}

[Description("Feature band entry in the downloads index")]
public record FeatureBandEntry(
    [Description("Feature band version (e.g., '8.0.4xx')")]
    string Version,
    [Description("Human-readable title for the feature band")]
    string Title,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support phase (active, eol)")]
    SupportPhase? SupportPhase)
{
    [JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this feature band's download file")]
    public Dictionary<string, HalLink>? Links { get; init; }
}

/// <summary>
/// Component download file with evergreen download links.
/// Located at {version}/downloads/{component}.json.
/// </summary>
[Description("Component download file with evergreen download links")]
public record ComponentDownload(
    [Description("Type of document, always 'component-download'")]
    ReleaseKind Kind,
    [Description("Component name (e.g., 'runtime', 'aspnetcore', 'windowsdesktop', 'sdk')")]
    string Component,
    [Description("Major version (e.g., '8.0', '9.0')")]
    string Version,
    [Description("Concise title for the document")]
    string Title)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the component download")]
    public string? Description { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Feature band version for SDK files (e.g., '8.0.4xx')")]
    public string? FeatureBand { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink>? Links { get; init; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded download file entries")]
    public ComponentDownloadEmbedded? Embedded { get; init; }
}

[Description("Container for component downloads keyed by runtime identifier")]
public record ComponentDownloadEmbedded(
    [Description("Dictionary of downloads keyed by RID (e.g., 'linux-x64', 'win-x64')")]
    Dictionary<string, DownloadFile> Downloads);

[Description("Individual download file for a specific platform")]
public record DownloadFile(
    [Description("File name")]
    string Name,
    [Description("Runtime identifier")]
    string Rid,
    [Description("Operating system")]
    string Os,
    [Description("Architecture")]
    string Arch,
    [Description("Hash algorithm used for file verification (e.g., 'sha512')")]
    string HashAlgorithm)
{
    [JsonPropertyName("_links"),
     Description("Download and hash links")]
    public Dictionary<string, HalLink>? Links { get; init; }
}
