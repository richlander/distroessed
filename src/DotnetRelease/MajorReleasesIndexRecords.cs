using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// For release-index.json file
// Index of major releases for a product
// Example: https://github.com/dotnet/core/blob/main/release-notes/releases-index.json
[Description("A set of major product releases with high-level information, like latest patch version.")]
public record MajorReleasesIndex(
    [property: Description("Set of major releases.")]
    IList<MajorReleaseIndexItem> ReleasesIndex);

[Description("A major version release, including the latest patch version, and whether that patch version included security updates.")]
public record MajorReleaseIndexItem(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("The version of the most recent patch release.")]
    string LatestRelease,
    
    [property: Description("The date of the most recent patch release.")]
    DateOnly LatestReleaseDate,
    
    [property: Description("Whether the most recent patch release contains security fixes.")]
    bool Security,
    
    [property: Description("The runtime version of the most recent patch release.")]
    string LatestRuntime,

    [property: Description("The SDK version of the most recent patch release.")]
    string LatestSdk,
    
    [property: Description("The product marketing name.")]
    string Product,
    
    [property: Description("The support phase of the major release.")]
    SupportPhase SupportPhase,

    [property: Description("End of life date of the major release.")]
    DateOnly EolDate,
    
    [property: Description("The release type for of the makor release.")]
    ReleaseType ReleaseType,

    [property: Description("Link to detailed release descriptions (JSON format), with all patch releases in one file. This property is now deprecated, but still required (for compatibility)."),
        JsonPropertyName("releases.json"),
        Obsolete("This property is obsolete. Use PatchReleasesInfoUri instead.")]
    string ReleasesJson,

    [property: Description("Link to detailed release descriptions (JSON format), with all patch releases in one file.")]
    string PatchReleasesInfoUri,

    [property: Description("Link to index file of detailed release descriptions (JSON format), with one file per patch release."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? PatchReleasesIndexUri = null,

    [property: Description("Link to supported OS matrix (JSON format). This property is now deprecated, but still required (for compatibility)."),
        JsonPropertyName("supported-os.json"),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
        Obsolete("This property is obsolete. Use SupportedOsInfoUri instead.")]
    string? SupportedOsJson = null,

    [property: Description("Link to supported OS matrix (JSON format)."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? SupportedOsInfoUri = null,

    [property: Description("Link to OS package information (JSON format)."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? OsPackagesInfoUri = null);

