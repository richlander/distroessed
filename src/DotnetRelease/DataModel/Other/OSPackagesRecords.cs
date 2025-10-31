using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// For os-packages.json file
// List of packages required by product
// Example: https://github.com/dotnet/core/blob/main/release-notes/9.0/supported-os.json
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Skip)]
[Description("The set of packages required by a given product version for a set of distros.")]
public record OSPackagesOverview(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("Set of nominal packages used by product, with descriptions.")]
    IList<Package> Packages, 
    
    [property: Description("Set of distributions where the product can be used.")]
    IList<Distribution> Distributions);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A nominal package is a distro-agnostic representation of a package, including the scenarios for which the package is required. A nominal package will be referenced by a distribution package, with a distribution-specific package name.")]
public record Package(
    [property: Description("ID of nominal package.")]
    string Id,
        
    [property: Description("Display name of nominal package.")]
    string Name,

    [property: Description("Required scenarios for which the package must be used.")]
    IList<Scenario> RequiredScenarios,

    [property: Description("Minimum required version of library.")]
    string? MinVersion = null,

    [property: Description("Related references.")]
    IList<string>? References = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("An operating system distribution, with required package install commands and specific packages for distribution releases.")]
public record Distribution(
    [Description("Name of the distribution, matching ID in /etc/os-release, however, the expectation is that this value starts with a capital letter (proper noun).")]
    string Name,
    
    [Description("Commands required to install packages within the distribution.")]
    IList<Command> InstallCommands,
    
    [Description("Releases for that distribution.")]    
    IList<DistroRelease> Releases);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A command to be run to install packages")]
public record Command(
    [Description("Whether the command needs to be run under sudo.")]    
    bool RunUnderSudo,
    
    [Description("The command to be run, like apt.")]    
    string CommandRoot,
        
    [Description("The command parts or arguments that need to be used.")]    
    IList<string>? CommandParts = null);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A distribution release with a list of packages to install.")]
public record DistroRelease(
    [Description("The name of the release, matching PRETTY_NAME in /etc/os-release.")]    
    string Name,
    
    [Description("The version number for the release, matching VERSION_ID in /etc/os-release.")]    
    string Release,
    
    [Description("The packages required by the distro release.")]    
    IList<DistroPackage> Packages);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
[Description("A distro archive package to install, with a reference to a logical package with more information.")]
public record DistroPackage(
    [property: Description("Reference to nominal package ID, providing access to required scenarios and other information.")]
    string Id,
    
    [property: Description("Package name in the distro archive.")]
    string Name);

[JsonConverter(typeof(SnakeCaseLowerStringEnumConverter<Scenario>))]
[Description("Scenarios relating to package dependencies. 'All' includes both CoreCLR and NativeAOT while 'Runtime' is intended to cover CoreCLR, only.")]
public enum Scenario
{
    All,
    Runtime,
    Https,
    Cryptography,
    Globalization,
    Kerberos
}
