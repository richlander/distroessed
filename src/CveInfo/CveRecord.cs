namespace CveInfo;

public record CveSet(string Date, IList<Cve> Cves, IList<Commit>? Commits, Source Source);

public record Cve(string Id, string Description, string Product, IList<Package> Packages, IList<string> Platforms)
{
    public string? Cvss { get; set; }

    public IList<string>? References { get; set; }
}

public record Package(string Name, string MinVulnerableVersion, string MaxVulnerableVersion, string FixedVersion);

public record Commit(string Cve, string Org, string Repo, string Branch, string Hash, string? Url);

public record Source(string Name, string CommitUrl, string BranchUrl);
