using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// For release-index.json file
// Index of patch release for a major version
// Example: https://github.com/dotnet/core/blob/main/release-notes/9.0/release-index.json
[Description("A set of product releases with high-level information, like latest version.")]
public record PatchReleasesIndex(string ChannelVersion, IList<PatchReleaseIndexItem> Releases);

public record PatchReleaseIndexItem(
    [property: Description("Version of release.")]
    string ReleaseVersion,

    [property: Description("Date of release.")]    
    DateOnly ReleaseDate,
    
    [property: Description("Security status of release.")]
    bool Security,

    [property: Description("Url to detailed description of release."),
            JsonPropertyName("release.json")]
    string ReleaseJson);

/*
{
    "channel-version": "8.0",
    "releases": [
        {
            "version": "8.0.7",
            "date": "2024-07-09",
            "security": true,
            "release.json": "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/8.0/8.0.1/release.json"
        }
    ]
}
*/
