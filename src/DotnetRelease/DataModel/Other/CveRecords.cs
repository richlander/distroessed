using System.ComponentModel;

namespace DotnetRelease;

[Description("Collection of CVE security vulnerability records for a specific date")]
public record CveRecords(
    [Description("Date when the CVEs were disclosed (ISO 8601 format)")]
    string Date,
    [Description("List of detailed CVE vulnerability records")]
    IReadOnlyList<CveRecord> Records,
    [Description("Platform components grouped by .NET version")]
    IReadOnlyDictionary<string, IReadOnlyList<CvePackageAffected>> Platform,
    [Description("NuGet packages grouped by package name")]
    IReadOnlyDictionary<string, IReadOnlyList<CvePackageAffected>> Packages)
{
    [Description("Git commits that address these CVE vulnerabilities")]
    public IReadOnlyList<Commit>? Commits { get; set; }
}

[Description("Detailed CVE security vulnerability record")]
public record CveRecord(
    [Description("CVE identifier (e.g., 'CVE-2025-12345')")]
    string Id,
    [Description("Title describing the vulnerability")]
    string Title)
{
    [Description("Severity rating (e.g., 'Critical', 'High', 'Medium', 'Low')")]
    public string? Severity { get; set; }
    [Description("CVSS (Common Vulnerability Scoring System) score")]
    public string? Cvss { get; set; }
    [Description("Detailed description of the vulnerability")]
    public IReadOnlyList<string>? Description { get; set; }
    [Description("Recommended mitigation steps")]
    public IReadOnlyList<string>? Mitigation { get; set; }
    [Description("Affected product name")]
    public string? Product { get; set; }
    [Description("List of platforms affected by this CVE (unset means all platforms)")]
    public IReadOnlyList<string>? Platforms { get; set; }
    [Description("External reference URLs for more information")]
    public IReadOnlyList<string>? References { get; set; }
}

[Description("Simplified CVE record for embedding in indexes")]
public record CveRecordSummary(
    [Description("CVE identifier (e.g., 'CVE-2025-12345')")]
    string Id,
    [Description("Title describing the vulnerability")]
    string Title)
{
    [Description("URL to detailed CVE information")]
    public string? Href { get; set; }
};

[Description("Collection of simplified CVE records")]
public record CveRecordsSummary(
    [Description("List of CVE summary records")]
    IReadOnlyList<CveRecordSummary> Records);

[Description("Package affected by CVE vulnerabilities (in new schema format)")]
public record CvePackageAffected(
    [Description("CVE identifier that affects this package")]
    string CveId,
    [Description("Minimum vulnerable version")]
    string MinVulnerable,
    [Description("Maximum vulnerable version")]
    string MaxVulnerable,
    [Description("Version where the vulnerability was fixed")]
    string Fixed,
    [Description("Version family affected (e.g., '8.0', '9.0')")]
    string Family,
    [Description("Component name")]
    string Component)
{
    [Description("Git commit hashes that fixed this CVE")]
    public IReadOnlyList<string>? Commits { get; set; }
    [Description("Specific binary files affected by the CVE")]
    public IReadOnlyList<string>? Binaries { get; set; }
}

[Description("Package affected by CVE vulnerabilities (legacy format)")]
public record CvePackage(
    [Description("Package name")]
    string Name,
    [Description("List of vulnerability details for this package")]
    IReadOnlyList<Affected> Affected);

[Description("Vulnerability details for a specific package version range (legacy format)")]
public record Affected(
    [Description("CVE identifier that affects this package")]
    string CveId,
    [Description("Minimum vulnerable version")]
    string MinVulnerable,
    [Description("Maximum vulnerable version")]
    string MaxVulnerable,
    [Description("Version where the vulnerability was fixed")]
    string Fixed)
{
    [Description("Specific binary files affected by the CVE")]
    public IReadOnlyList<string>? Binaries { get; set; }
    [Description("Version family affected (e.g., '8.0', '8.0.100x')")]
    public string? Family { get; set; }
    [Description("Git commit hashes that fixed this CVE")]
    public IReadOnlyList<string>? Commits { get; set; }
}

[Description("Git commit information for CVE fixes")]
public record Commit(
    [Description("Repository name")]
    string Repo,
    [Description("Git branch name")]
    string Branch,
    [Description("Git commit hash")]
    string Hash)
{
    [Description("GitHub organization or user name")]
    public string? Org { get; set; }
    [Description("Full URL to the commit")]
    public string? Url { get; set; }
};
