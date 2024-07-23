using System.ComponentModel;

namespace DotnetRelease;

// For supported-os.json file
// Set of operating systems supported by product
// Example: https://github.com/dotnet/core/blob/main/release-notes/9.0/supported-os.json
[Description("Operating system support matrix for a given major product version.")]
public record SupportedOSMatrix(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("Date when file was last updated")]
    DateOnly LastUpdated,

    [property: Description("Supported operating system families.")]
    IList<SupportFamily> Families)
    {
        [Description("Minimum supported libc versions, per architecture.")]
        public IList<SupportLibc>? Libc { get; set; } = null;

        [Description("Notes relating to support.")]
        public IList<string>? Notes { get; set; } = null;
    };

[Description("Operating system family, such as Linux.")]
public record SupportFamily(
    [property: Description("Operating system family name.")]
    string Name,
    
    [property: Description("Supported operating system family distributions.")]
    IList<SupportDistribution> Distributions); 

[Description("A supported operating system distribution, like iOS or Ubuntu.")]
public record SupportDistribution(
    [property: Description("ID for distribution matching IDs used at https://endoflife.date/.")]
    string Id,
    
    [property: Description("Display name for distribution.")]
    string Name,
    
    [property: Description("Link to home page for distribution.")]
    string Link,
    
    [property: Description("Supported architectures for distribution.")]
    IList<string> Architectures, 

    [property: Description("Supported versions for distribution.")]
    IList<string> SupportedVersions)
{
    [Description("Once but not longer supported versions for distribution.")]
    public IList<string>? UnsupportedVersions { get; set; } = null;

    [Description("Link to lifecycle page for distribution.")]
    public string? Lifecycle { get; set; } = null;

    [Description("Support notes for distribution. For example, use notes if a given distribution architecture or version is only supported in certain circumstances.")]
    public IList<string>? Notes { get; set; } = null;
}

[Description("Minimum supported libc versions, for both glibc and musl, with the allowance for different versions per architecture.")]
public record SupportLibc(
    [property: Description("Name of libc library.")]
    string Name,

    [property: Description("Minimum version supported.")]
    string Version,

    [property: Description("Architectures where that version is supported.")]
    IList<string> Architectures,
    
    [property: Description("Source of the libc header files and libraries (used by a compiler).")]    
    string Source);
