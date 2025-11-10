using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Graph;

namespace DotnetRelease.Security;

[Description("Simplified CVE record for embedding in indexes")]
public record CveRecordSummary(
    [Description("CVE identifier (e.g., 'CVE-2025-12345')")]
    string Id,
    [Description("Title describing the vulnerability")]
    string Title)
{
    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("HAL+JSON links to related CVE resources")]
    public Dictionary<string, object>? Links { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("List of fix commit links that resolve this CVE")]
    public IList<CommitLink>? Fixes { get; set; }

    [Description("CVSS base score"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? CvssScore { get; set; }

    [Description("CVSS severity rating (e.g., 'HIGH', 'CRITICAL')"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CvssSeverity { get; set; }

    [Description("Date when the CVE was publicly disclosed"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DateOnly? DisclosureDate { get; set; }

    [Description("List of .NET major versions affected by this CVE"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? AffectedReleases { get; set; }

    [Description("List of products affected by this CVE"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? AffectedProducts { get; set; }

    [Description("List of packages affected by this CVE"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? AffectedPackages { get; set; }

    [Description("Platforms affected by the CVE (e.g., 'linux', 'windows', 'all')"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<string>? Platforms { get; set; }
}

[Description("Link to a Git commit with repository context")]
public record CommitLink(
    [Description("URL to the commit diff")]
    string Href,
    [Description("Repository name (e.g., 'aspnetcore', 'runtime')")]
    string Repo,
    [Description("Branch name (e.g., 'release/8.0')")]
    string Branch)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Descriptive title for the commit")]
    public string? Title { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description(".NET release version (e.g., '8.0')")]
    public string? Release { get; set; }
}

[Description("Collection of simplified CVE records")]
public record CveRecordsSummary(
    [Description("List of CVE summary records")]
    IReadOnlyList<CveRecordSummary> Records);
