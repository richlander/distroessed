namespace DotnetRelease;

public record CveRecords(string Date, IReadOnlyList<CveRecord> Records, IReadOnlyList<CvePackage> Packages)
{
    public IReadOnlyList<Commit>? Commits { get; set; }
}

public record CveRecord(string Id, string Title)
{
    public string? Severity { get; set; }
    public string? Cvss { get; set; }
    public IReadOnlyList<string>? Description { get; set; }
    public IReadOnlyList<string>? Mitigation { get; set; }
    public string? Product { get; set; }
    // This is the list of platforms affected by the CVE
    // Unset is assumed to be all platforms
    public IReadOnlyList<string>? Platforms { get; set; }
    public IReadOnlyList<string>? References { get; set; }
}

[System.ComponentModel.Description("Summary of a CVE record with identifier, title, and optional link.")]
public record CveRecordSummary(string Id, string Title)
{
    public string? Href { get; set; }
};

public record CveRecordsSummary(IReadOnlyList<CveRecordSummary> Records);

public record CvePackage(string Name, IReadOnlyList<Affected> Affected);
public record Affected(string CveId, string MinVulnerable, string MaxVulnerable, string Fixed)
{
    // May be important to specify the binaries affected by the CVE
    public IReadOnlyList<string>? Binaries { get; set; }
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
