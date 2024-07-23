using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public record MajorReleaseOverview(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("The version of the most recent patch release for the channel-version.")]
    string LatestRelease,
    
    [property: Description("The date of the most recent release date for the channel-version.")]
    string LatestReleaseDate,
    
    [property: Description("The runtime version of the most recent patch release for the channel-version.")]
    string LatestRuntime,

    [property: Description("The SDK version of the most recent patch release for the channel-version.")]
    string LatestSdk,
    
    [property: Description("The current support phase for the channel-version.")]
    SupportPhase SupportPhase,
    
    [property: Description("The release type for a .NET version.")]
    ReleaseType ReleaseType,
    
    [property: Description("Link to lifecycle page for product.")]
    string Lifecycle,
    
    [property: Description("A set of patch releases with detailed release information.")]
    IList<PatchRelease> Releases    
    );

public record PatchRelease(

    [property: Description("The date of the patch release.")]
    string ReleaseDate,

    [property: Description("The version of the release")]
    string ReleaseVersion,

    [property: Description("The security status of the release.")]
    bool Security,

    [property: Description("The CVEs disclosed with the release.")]
    IList<Cve> CveList,

    [property: Description("A URL to release notes.")]
    string ReleaseNotes,

    [property: Description("Runtime component of release.")]
    RuntimeComponent Runtime,

    [property: Description("SDK component of release (primary SDK release).")]
    SdkComponent Sdk,

    [property: Description("SDK components of release (often multiple, otherwise a repeat of `Sdk` value within an array).")]
    IList<SdkComponent> Sdks,

    [property: Description("ASP.NET Core component of release"),
            JsonPropertyName("aspnetcore-runtime")]
    AspNetCoreComponent AspNetCoreRuntime,

    [property: Description("Windows Desktop component of release.")]
    Component WindowsDesktop

);

[Description("A disclosed vulnerability (AKA CVE).")]
public record Cve(
    [property: Description("The ID tracking the CVE.")]
    string CveId,
    
    [property: Description("The URL tracking the CVE at the authoritative site.")]
    string CveUrl);

[Description("Runtime component of a release.")]
public record RuntimeComponent(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The version of Visual Studio that includes this component version.")]
    string VSVersion,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("SDK component of a release.")]
public record SdkComponent(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The version of Visual Studio that includes this component version.")]
    string VSVersion,

    [property: Description("The version of C# included in this component version.")]
    string CSharpVersion,

    [property: Description("The version of F# included in this component version.")]
    string FSharpVersion,

    [property: Description("The version of Visual Basic included in this component version.")]
    string VBVersion,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("ASP.NET Core component of a release.")]
public record AspNetCoreComponent(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The version of Visual Studio that includes this component version."),
            JsonPropertyName("version-aspnetcoremodule")]
    IList<string> VersionAspNetCoreModule,

    [property: Description("The version of Visual Studio that includes this component version.")]
    string VSVersion,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("Arbitrary component of a release.")]
public record Component(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

public record ComponentFile(
    [property: Description("Name of file.")]
    string Name,
    
    [property: Description("Runtime ID of file, descriptibing OS and architecture applicability, like `linux-x64`.")]
    string Rid,
    
    [property: Description("Fully-qualified URL of file.")]
    string Url,
    
    [property: Description("Content hash of file.")]
    string Hash);
