using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Graph;

namespace DotnetRelease.Security;

/// <summary>
/// Simplified CVE record for embedding in timeline indexes.
/// For detailed fix information (commits, version ranges), use cve.json.
/// </summary>
[Description("Simplified CVE record for embedding in indexes. For fix details, follow cve-json link.")]
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

[Description("Collection of simplified CVE records")]
public record CveRecordsSummary(
    [Description("List of CVE summary records")]
    IReadOnlyList<CveRecordSummary> Records);
