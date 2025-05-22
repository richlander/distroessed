namespace CveInfo;

public record CveSet(string Date, IReadOnlyList<Cve> Cves, IReadOnlyList<Commit>? Commits, Source Source);

public record Cve(string Id, string Description, string Product, IReadOnlyList<Package> Packages, IReadOnlyList<string> Platforms)
{
    public string? Cvss { get; set; }

    public IReadOnlyList<string>? References { get; set; }
}

public record Package(string Name, IReadOnlyList<VersionRange> Versions);

public record VersionRange(string MinVulnerable, string MaxVulnerable, string Fixed, string? MajorVersion);

public record Commit(string Cve, string Org, string Repo, string Branch, string Hash, string? Url, string? MajorVersion);

public record Source(string Name, string CommitUrl, string BranchUrl);
