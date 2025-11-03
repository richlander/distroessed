using System.ComponentModel;

namespace DotnetRelease;

[Description("Simplified CVE record for embedding in indexes")]
public record CveRecordSummary(
    [Description("CVE identifier (e.g., 'CVE-2025-12345')")]
    string Id,
    [Description("Title describing the vulnerability")]
    string Title)
{
    [Description("URL to detailed CVE information")]
    public string? Href { get; set; }
}

[Description("Collection of simplified CVE records")]
public record CveRecordsSummary(
    [Description("List of CVE summary records")]
    IReadOnlyList<CveRecordSummary> Records);
