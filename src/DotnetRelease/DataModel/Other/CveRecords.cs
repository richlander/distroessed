using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Collection of CVE security vulnerability records for a specific date")]
public record CveRecords(
    [Description("Date when the CVEs were disclosed (ISO 8601 format)")]
    string Date,
    [Description("Array of CVE metadata objects")]
    IReadOnlyList<CveRecord> Cves,
    [Description("Array of product vulnerability entries (flat, denormalized)")]
    IReadOnlyList<ProductEntry> Products,
    [Description("Array of extension vulnerability entries (flat, denormalized)")]
    IReadOnlyList<ExtensionEntry> Extensions,
    [Description("Dictionary of commit details keyed by commit hash")]
    IReadOnlyDictionary<string, Commit> Commits)
{
    [JsonPropertyName("product-names")]
    [Description("Maps product identifiers to human-readable display names")]
    public IReadOnlyDictionary<string, string>? ProductNames { get; set; }
    
    [JsonPropertyName("product-cves")]
    [Description("Maps product identifiers to CVE IDs")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ProductCves { get; set; }
    
    [JsonPropertyName("release-cves")]
    [Description("Maps release families to affected CVEs")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? ReleaseCves { get; set; }
    
    [JsonPropertyName("cve-releases")]
    [Description("Maps CVE IDs to affected release families")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? CveReleases { get; set; }
    
    [JsonPropertyName("cve-commits")]
    [Description("Maps CVE IDs to commit hashes")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? CveCommits { get; set; }
}

[Description("Detailed CVE security vulnerability record")]
public record CveRecord(
    [Description("CVE identifier (e.g., 'CVE-2024-12345')")]
    string Id,
    [Description("Brief description of the vulnerability problem")]
    string Problem,
    [Description("Severity rating (e.g., 'Critical', 'High', 'Medium', 'Low')")]
    string Severity)
{
    [Description("CVSS (Common Vulnerability Scoring System) information")]
    public CvssInfo? Cvss { get; set; }
    [Description("Detailed description of the vulnerability")]
    public IReadOnlyList<string>? Description { get; set; }
    [Description("CNA (CVE Numbering Authority) that assigned this CVE")]
    public string? Cna { get; set; }
    [Description("List of platforms affected by this CVE (unset means all platforms)")]
    public IReadOnlyList<string>? Platforms { get; set; }
    [Description("External reference URLs for more information")]
    public IReadOnlyList<string>? References { get; set; }
}

[Description("CVSS scoring information")]
public record CvssInfo(
    [Description("CVSS version (e.g., '3.1')")]
    string Version,
    [Description("CVSS vector string URI")]
    string Uri);

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

[Description("Product entry in flat denormalized format")]
public record ProductEntry(
    [property: JsonPropertyName("cve-id")]
    [property: Description("CVE identifier this entry relates to")]
    string CveId,
    [property: Description("Product identifier for lookup and grouping")]
    string Name,
    [property: JsonPropertyName("min-vulnerable")]
    [property: Description("Minimum vulnerable version")]
    string MinVulnerable,
    [property: JsonPropertyName("max-vulnerable")]
    [property: Description("Maximum vulnerable version")]
    string MaxVulnerable,
    [property: Description("Version where the vulnerability was fixed")]
    string Fixed,
    [property: Description("Release family like '8.0'")]
    string Release)
{
    [Description("Array of commit hashes referencing the commits dictionary")]
    public IReadOnlyList<string>? Commits { get; set; }
}

[Description("Extension entry in flat denormalized format")]
public record ExtensionEntry(
    [property: JsonPropertyName("cve-id")]
    [property: Description("CVE identifier this entry relates to")]
    string CveId,
    [property: Description("Extension identifier for lookup and grouping")]
    string Name,
    [property: JsonPropertyName("min-vulnerable")]
    [property: Description("Minimum vulnerable version")]
    string MinVulnerable,
    [property: JsonPropertyName("max-vulnerable")]
    [property: Description("Maximum vulnerable version")]
    string MaxVulnerable,
    [property: Description("Version where the vulnerability was fixed")]
    string Fixed)
{
    [Description("Release family like '8.0' (optional for some extensions)")]
    public string? Release { get; set; }
    [Description("Array of commit hashes referencing the commits dictionary")]
    public IReadOnlyList<string>? Commits { get; set; }
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
    string Hash,
    [Description("GitHub organization or user name")]
    string Org,
    [Description("Full URL to the commit diff (ends with .diff)")]
    string Url);
