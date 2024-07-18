using System.ComponentModel;

namespace DotnetRelease;

[Description("The set of packages required by a given .NET version for a set of distros.")]
public record PackageOverview(string ChannelVersion, DateOnly EolDate, IList<Package> Packages, IList<Distribution> Distributions);

[Description("A logical package that will exist in various distros with different package names. Includes the scenarios for which the package is required.")]
public record Package(string Id, string Name, IList<string>? References, IList<string>? Required);

[Description("An operating system distribution, with required package install commands and specific packages for distribution releases.")]
public record Distribution(string Name, IList<Command>? InstallCommands, IList<DistroRelease> Releases);

[Description("A command to be run to install packages")]
public record Command(bool RunUnderSudo, string CommandRoot, IList<string> CommandParts);

[Description("A distribution release with a list of packages to install.")]
public record DistroRelease(string Name, string Release, IList<DistroPackage> Packages);

[Description("A distro archive package to install, with a reference to a logical package with more information.")]
public record DistroPackage(string Id, string Name);
