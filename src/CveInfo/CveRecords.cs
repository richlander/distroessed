namespace CveInfo;

public record CveRecords(string Date, IReadOnlyList<Cve> Records, IReadOnlyList<Package> Packages)
{
    public IReadOnlyList<Commit>? Commits { get; set; }
}

public record Cve(string Id, string Title)
{
    public IReadOnlyList<string>? Description { get; set; }
    public string? Cvss { get; set; }
    public string? Product { get; set; }
    // This is the list of platforms affected by the CVE
    // Unset is assumed to be all platforms
    public IReadOnlyList<string>? Platforms { get; set; }
    public IReadOnlyList<string>? References { get; set; }
}

public record Package(string Name, IReadOnlyList<Affected> Affected);

public record Affected(string CveId, string MinVulnerable, string MaxVulnerable, string Fixed)
{
    // This is the version family affected
    // Might be "8.0", "8.0.100x", a codename or whatever is appropriate
    public string? Family { get; set; }
    // This is the commit that fixed the CVE (can be multiple)
    // Can be used as a foreign key to CveRecords.Commits
    public IReadOnlyList<string>? Commits { get; set; }
}

public record Commit(string Repo, string Branch, string Hash)
{
    public string? Org { get; set; }
    public string? Url { get; set; }
};
