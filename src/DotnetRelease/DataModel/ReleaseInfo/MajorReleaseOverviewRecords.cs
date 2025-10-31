using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

//[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A major product release, including detailed information for each patch release.")]
public record MajorReleaseOverview(
    [property: Description("Major (or major.minor) version of the product.")]
    string ChannelVersion,

    [property: Description("The version of the most recent patch release.")]
    string LatestRelease,

    [property: Description("The date of the most recent release.")]
    DateOnly LatestReleaseDate,

    [property: Description("The runtime version of the most recent patch release.")]
    string LatestRuntime,

    [property: Description("The SDK version of the most recent patch release.")]
    string LatestSdk,

    [property: Description("The current support phase of the major version.")]
    SupportPhase SupportPhase,

    [property: Description("The release type of the major version.")]
    ReleaseType ReleaseType,

    [property: Description("The end of life (EOL) date of the major version.")]
    DateOnly EolDate,

    [property: Description("Link to lifecycle page for the product.")]
    string LifecyclePolicy,

    [property: Description("A set of patch releases with detailed release information.")]
    IList<PatchRelease> Releases,


    [property: Description("Intellisense files for the release."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    Intellisense? Intellisense = null
    );


public record Intellisense(
    string Version,
    string VersionDisplay,
    IList<IntellisenseFile> Files
);


public record IntellisenseFile(
    string Lang,
    string Name,
    string Rid,
    string Url,
    string Hash
);
