using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

[Description("Index of .NET releases for a specific year, organized by months")]
public record HistoryYearIndex(
    [Description("Type of history index document")]
    HistoryKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the year's releases")]
    string Description,
    [Description("Year identifier (e.g., '2025')")]
    string Year,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{

    [JsonPropertyName("_embedded"),
     Description("Embedded monthly summaries and release listings")]
    public HistoryYearIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded year-level navigation entries")]
public record HistoryYearIndexEmbedded
{
    [Description("Monthly release summaries for this year")]
    public List<HistoryMonthSummary>? Months { get; set; }
    [Description("All release versions that occurred during this year")]
    public List<ReleaseHistoryIndexEntry>? Releases { get; set; }
}

[Description("Container for embedded monthly navigation entries")]
public record MonthIndexEmbedded(
    [Description("List of month entries with navigation links")]
    List<HistoryMonthEntry> Months);

[Description("Detailed month entry with full release and CVE information")]
public record HistoryMonthEntry(
    [Description("Month identifier (e.g., '02' for February)")]
    string Month,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this month's content")]
    Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("CVE security vulnerability records for this month")]
    IReadOnlyList<CveRecordSummary>? CveRecords,
    [Description("List of .NET major version identifiers that had releases this month")]
    IList<string> DotnetReleases,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("List of specific patch version identifiers released this month")]
    IList<string>? DotnetPatchReleases
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE summary information by version")]
    public IList<HistoryCveInfo>? CveInfo { get; set; }
};

[Description("Simplified month entry for year-level summaries")]
public record HistoryMonthSummary(
    [Description("Month identifier (e.g., '02' for February)")]
    string Month,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this month's content")]
    Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("CVE security vulnerability records for this month")]
    IReadOnlyList<CveRecordSummary>? CveRecords,
    [Description("List of .NET major version identifiers that had releases this month")]
    IList<string> DotnetReleases
);

[Description("Index of .NET releases for a specific month")]
public record HistoryMonthIndex(
    [Description("Type of history index document")]
    HistoryKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the month's releases")]
    string Description,
    [Description("Year identifier (e.g., '2025')")]
    string Year,
    [Description("Month identifier (e.g., '02' for February)")]
    string Month,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{

    [JsonPropertyName("_embedded"),
     Description("Embedded release listings for this month")]
    public HistoryMonthIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded month-level release entries")]
public record HistoryMonthIndexEmbedded
{
    [Description("Releases grouped by major version with patch releases and links (keyed by version)")]
    public Dictionary<string, MajorReleaseHistory>? Releases { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability disclosures for this month")]
    public IReadOnlyList<CveRecordSummary>? Disclosures { get; set; }
}

[Description("Release history for a major version during a specific time period")]
public record MajorReleaseHistory(
    [Description("Patch releases grouped by component type (dotnet-runtime, dotnet-sdk, etc.)")]
    Dictionary<string, IList<string>> Patches)
{
    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("HAL+JSON links to version index and release resources")]
    public Dictionary<string, object>? Links { get; set; }
}

[Description("CVE summary information for a specific version")]
public record HistoryCveInfo(
    [Description("Version identifier affected by CVEs")]
    string Version,
    [Description("Number of CVEs affecting this version")]
    int CveCount);

[Description("Release metadata with navigation information")]
public record ReleaseMetadata(
    [Description("Version identifier")]
    string Version,
    [Description("URL to the release information")]
    string Href,
    [Description("Title of the release")]
    string Title,
    [Description("MIME type of the linked resource")]
    string Type);

/*
{
    "kind": "history-year-index",
    "description": "2025 Release History Index",
    "year": "2025",
    "_links": {
        "self": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/archives/2025/index.json",
            "relative": "index.json",
            "title": "2025 Release History Index",
            "type": "application/hal+json"
            }
        },
    "_embedded": {
        "entries": [
            {
                "kind": "history-month-index",
                "description": "Releases in February 2025 for .NET 8, 9, and 10",
                "year": "2025",
                "month": "02",
                "_links": {
                    "self": { ... },
                    "cve-json": { ... },
                    "cve-markdown": { ... }
                },
                "dotnet-releases": [
                    { "version": "8.0", "cve-count": 2 },
                    { "version": "9.0", "cve-count": 1 },
                    { "version": "10.0", "cve-count": 0 }
                ],
                "cve-count": 2
            }
        ]
    }
}
*/
