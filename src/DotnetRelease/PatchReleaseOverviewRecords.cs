using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public record PatchReleaseOverview(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("The date of the patch release.")]
    DateOnly ReleaseDate,

    [property: Description("The version (branding) of the release")]
    string ReleaseVersion,

    [property: Description("Whether the release contains any CVE fixes.")]
    bool Security,

    [property: Description("A patch release with detailed release information.")]
    PatchRelease Release);

public record PatchRelease(
    [property: Description("The date of the patch release.")]
    DateOnly ReleaseDate,

    [property: Description("The version (branding) of the release")]
    string ReleaseVersion,

    [property: Description("Whether the release contains any CVE fixes.")]
    bool Security,

    [property: Description("The CVEs disclosed with the release.")]
    IList<Cve> CveList,

    [property: Description("A URL to release notes.")]
    string ReleaseNotes,

    [property: Description("Runtime component of the release.")]
    RuntimeComponent Runtime,

    [property: Description("SDK component of the release (primary SDK release).")]
    SdkComponent Sdk,

    [property: Description("SDK components of the release (often multiple, otherwise a repeat of `Sdk` value within an array).")]
    IList<SdkComponent> Sdks,

    [property: Description("ASP.NET Core component of the release"),
        JsonPropertyName("aspnetcore-runtime")]
    AspNetCoreComponent AspNetCoreRuntime,

    [property: Description("Windows Desktop component of the release."),
        JsonPropertyName("windowsdesktop")]
    Component WindowsDesktop);

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

    [property: Description("The display or branding variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The minimum version of Visual Studio that supports this component version."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string VSSupport,

    [property: Description("The version of Visual Studio that includes this component version."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string VSVersion,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("SDK component of the release.")]
public record SdkComponent(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display or branding variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The version of the runtime that is included in the component.")]
    string RuntimeVersion,

    [property: Description("The version of Visual Studio that includes this component version.")]
    string VSVersion,

    [property: Description("The minimum version of Visual Studio that supports this component version.")]
    string VSSupport,

    [property: Description("The version of C# included in the component."),
        JsonPropertyName("csharp-version")]
    string CSharpVersion,

    [property: Description("The version of F# included in the component."),
        JsonPropertyName("fsharp-version")]
    string FSharpVersion,

    [property: Description("The version of Visual Basic included in the component.")]
    string VBVersion,

    [property: Description("The files that are available for the component.")]
    IList<ComponentFile> Files
);

[Description("ASP.NET Core component of the release.")]
public record AspNetCoreComponent(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The version of the ASP.NET Core module (ANCM) that is part of the component."),
        JsonPropertyName("version-aspnetcoremodule")]
    IList<string> VersionAspNetCoreModule,

    [property: Description("The version of Visual Studio that includes this component version.")]
    string VSVersion,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("Component that is part of the release.")]
public record Component(
    [property: Description("The version of the component.")]
    string Version,

    [property: Description("The display or branding variant of the version, if different.")]
    string VersionDisplay,

    [property: Description("The files that are available for this component.")]
    IList<ComponentFile> Files
);

[Description("File that is part of a release.")]
public record ComponentFile(
    [property: Description("File name.")]
    string Name,
    
    [property: Description("Runtime ID of file, describing OS and architecture applicability, like `linux-x64`.")]
    string Rid,
    
    [property: Description("Fully-qualified URL of file.")]
    string Url,
    
    [property: Description("Short-link URL to file."),
        JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string Akams,

    [property: Description("Content hash of file.")]
    string Hash);
