using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Index of .NET releases for a specific year with month-by-month breakdown.")]
public record HistoryYearIndex(HistoryKind Kind, string Description, string Year, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("_embedded")]
    public HistoryYearIndexEmbedded? Embedded { get; set; }
}

public record HistoryYearIndexEmbedded
{
    public List<HistoryMonthSummary>? Months { get; set; }
    public List<HistoryReleaseIndexEntry>? Releases { get; set; }
}

public record MonthIndexEmbedded(List<HistoryMonthEntry> Months);

public record HistoryMonthEntry(
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CveRecordSummary>? CveRecords,
    IList<string> DotnetReleases,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IList<string>? DotnetPatchReleases
)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IList<HistoryCveInfo>? CveInfo { get; set; }
};

// Simplified month entry for year index
public record HistoryMonthSummary(
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<CveRecordSummary>? CveRecords,
    IList<string> DotnetReleases
);

// Monthly index record
public record HistoryMonthIndex(
    HistoryKind Kind,
    string Description,
    string Year,
    string Month,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("_embedded")]
    public HistoryMonthIndexEmbedded? Embedded { get; set; }
}

public record HistoryMonthIndexEmbedded
{
    public IList<string>? DotnetReleases { get; set; }
    public IList<string>? DotnetPatchReleases { get; set; }
}

public record HistoryCveInfo(string Version, int CveCount);

// public record HistoryRelease(string Version, int CveCount, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links);

public record ReleaseMetadata(string Version, string Href, string Title, string Type);

/*
{
    "kind": "history-year-index",
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