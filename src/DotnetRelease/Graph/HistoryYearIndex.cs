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
    string Year)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest month with .NET releases in this year")]
    public string? LatestMonth { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest month with security releases in this year")]
    public string? LatestSecurityMonth { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest patch release version in this year (e.g., '10.0.0')")]
    public string? LatestRelease { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Major versions with releases in this year (e.g., ['10.0', '9.0', '8.0'])")]
    public IList<string>? Releases { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_embedded"),
     Description("Embedded monthly summaries and release listings")]
    public HistoryYearIndexEmbedded? Embedded { get; set; }

    [JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded year-level navigation entries")]
public record HistoryYearIndexEmbedded
{
    [Description("Monthly release summaries for this year")]
    public List<HistoryMonthSummary>? Months { get; set; }
    [Description("Major versions with releases during this year, with full lifecycle information")]
    public List<MajorReleaseVersionIndexEntry>? Releases { get; set; }
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
    IList<string> Releases,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("List of specific patch version identifiers released this month")]
    IList<string>? PatchReleases
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
    [Description("True if any release this month includes security fixes")]
    bool Security,
    [Description("Number of CVEs disclosed this month")]
    int CveCount,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), Description("CVE identifiers for this month")]
    IList<string>? CveRecords,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest major version with releases this month (e.g., '10.0')")]
    string? LatestRelease,
    [Description("List of .NET major version identifiers that had releases this month")]
    IList<string> Releases,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Runtime patch versions released this month (e.g., ['10.0.0', '9.0.11', '8.0.22'])")]
    IList<string>? RuntimePatches,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this month's content")]
    Dictionary<string, HalLink> Links
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
    [Description("True if any release this month includes security fixes")]
    bool Security)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Number of CVEs disclosed this month")]
    public int? CveCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers disclosed this month (for quick enumeration)")]
    public IList<string>? CveRecords { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest patch release version in this month (e.g., '10.0.0')")]
    public string? LatestRelease { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Major versions with releases in this month (e.g., ['10.0', '9.0', '8.0'])")]
    public IList<string>? Releases { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonPropertyName("_embedded"),
     Description("Embedded release listings for this month")]
    public HistoryMonthIndexEmbedded? Embedded { get; set; }

    [JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded month-level release entries")]
public record HistoryMonthIndexEmbedded
{
    [Description("Patch releases this month (symmetric with major version index structure)")]
    public List<PatchReleaseVersionIndexEntry>? Releases { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability disclosures for this month")]
    public IReadOnlyList<CveRecordSummary>? Disclosures { get; set; }
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
            "href": "https://raw.githubusercontent.com/dotnet/core/main/release-notes/archives/2025/index.json",
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
