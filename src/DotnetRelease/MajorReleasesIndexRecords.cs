using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// For release-index.json file
// Index of major releases for a product
// Example: https://github.com/dotnet/core/blob/main/release-notes/releases-index.json
[Description("A set of product releases with high-level information, like latest version.")]
public record MajorReleasesIndex(IList<MajorReleaseIndexItem> ReleasesIndex);

[Description("A major.minor version release, including atest patches, and whether that patch version included security updates.")]
public record MajorReleaseIndexItem(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("The version of the most recent patch release for the channel-version.")]
    string LatestRelease,
    
    [property: Description("The date of the most recent release date for the channel-version.")]
    string LatestReleaseDate,
    
    [property: Description("The security status of the most recent release date for the channel-version.")]
    bool Security,
    
    [property: Description("The runtime version of the most recent patch release for the channel-version.")]
    string LatestRuntime,

    [property: Description("The SDK version of the most recent patch release for the channel-version.")]
    string LatestSdk,
    
    [property: Description("The product name.")]
    string Product,
    
    [property: Description("The current support phase for the channel-version.")]
    SupportPhase SupportPhase,

    [property: Description("End of life date of .NET version.")]
    string EolDate,
    
    [property: Description("The release type for a .NET version.")]
    ReleaseType ReleaseType,

    [property: Description("Url to detailed release descriptions, with all patch releases in one file."),
            JsonPropertyName("releases.json")]
    string ReleasesJson,

    [property: Description("Url to index file of detailed release descriptions, with one file per patch release."),
            JsonPropertyName("releases-index.json")]
    string? ReleasesIndexJson = null,

    [property: Description("Url to supported OS matrix."),
            JsonPropertyName("supported-os.json")]
    string? SupportedOsJson = null);
