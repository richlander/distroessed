using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// For release-index.json file
// Index of patch release for a major version
// Example: https://github.com/dotnet/core/blob/main/release-notes/9.0/release-index.json
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A set of product patch releases with high-level information, like version and whether the release contains security fixes.")]
public record PatchReleasesIndex(
    [property: Description("Major (or major.minor) version of the product.")]
    string ChannelVersion,

    [property: Description("The version (branding) of the most recent patch release.")]
    string LatestRelease,
    
    [property: Description("The date of the most recent patch release.")]
    DateOnly LatestReleaseDate,

    [property: Description("Wehther the latest release includes security fixes.")]
    bool LatestReleaseSecurity,

    [property: Description("Link to supported OS matrix (JSON format)."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? SupportedOsInfoUri,

    [property: Description("Link to OS package information (JSON format)."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? OsPackagesInfoUri,

    [property: Description("Set of patch releases.")]
    IList<PatchReleaseIndexItem> Releases);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A patch release, including the version, and whether it included security updates.")]
public record PatchReleaseIndexItem(
    [property: Description("Version (branding) of the release.")]
    string ReleaseVersion,

    [property: Description("Date of release.")]    
    DateOnly ReleaseDate,
    
    [property: Description("Whether the release contains any CVE fixes.")]
    bool Security,

    [property: Description("Link to detailed description of the release.")]
    string ReleaseInfoUri);
