namespace DotnetRelease;

public record PackageOverview(string ChannelVersion, DateOnly EolDate, IList<Package> Packages, IList<Distribution> Distributions);

public record Package(string Id, string Name, IList<string>? References, IList<string>? Required);

public record Distribution(string Name, IList<string>? Commands, IList<DistroRelease> Releases);

public record DistroRelease(string Name, string Release, IList<DistroPackage> Packages);

public record DistroPackage(string Id, string Name);
