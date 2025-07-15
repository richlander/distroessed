using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseHistoryYearIndex(ReleaseHistoryKind Kind, string Description, string Year, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded")]
    public ReleaseHistoryYearIndexEmbedded? Embedded { get; set; }
}

public record ReleaseHistoryYearIndexEmbedded
{
    public List<ReleaseHistoryMonthSummary>? Months { get; set; }
    public List<ReleaseHistoryReleaseIndexEntry>? Releases { get; set; }
}

public record ReleaseHistoryMonthIndexEmbedded(List<ReleaseHistoryMonthEntry> Months);

public record ReleaseHistoryMonthEntry(
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CveRecordSummary>? CveRecords,
    IList<string> DotnetReleases,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IList<string>? DotnetPatchReleases
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<ReleaseHistoryCveInfo>? CveInfo { get; set; }
};

// Simplified month entry for year index
public record ReleaseHistoryMonthSummary(
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CveRecordSummary>? CveRecords,
    IList<string> DotnetReleases
);

// Monthly index record
public record ReleaseHistoryMonthIndex(
    ReleaseHistoryKind Kind,
    string Description,
    string Year,
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded")]
    public ReleaseHistoryMonthIndexEmbedded? Embedded { get; set; }
}

public record ReleaseHistoryMonthIndexEmbedded
{
    public IList<string>? DotnetReleases { get; set; }
    public IList<string>? DotnetPatchReleases { get; set; }
}

public record ReleaseHistoryCveInfo(string Version, int CveCount);

// public record ReleaseHistoryRelease(string Version, int CveCount, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links);

public record ReleaseMetadata(string Version, string Href, string Title, string Type);

/*
{
    "kind": "release-history-year-index",
    "description": "2025 Release History Index",
    "year": "2025",
    "_links": {
        "self": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/history/2025/index.json",
            "relative": "index.json",
            "title": "2025 Release History Index",
            "type": "application/hal+json"
            }
        },
    "_embedded": {
        "entries": [
            {
                "kind": "release-history-month-index",
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