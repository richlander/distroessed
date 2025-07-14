using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Index of .NET release history organized chronologically with HAL+JSON hypermedia links.")]
public record HistoryIndex(HistoryKind Kind, string Description, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("_embedded")]
    public HistoryIndexEmbedded? Embedded { get; set; }
}

[Description("Container for embedded history entries in a history index.")]
public record HistoryIndexEmbedded
{
    public List<HistoryYearEntry>? Years { get; set; }
    public List<HistoryReleaseIndexEntry>? Releases { get; set; }
}

[Description("Release entry in history index with version and navigation links.")]
public record HistoryReleaseIndexEntry(string Version, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links);

public record YearIndexEmbedded(List<HistoryYearEntry> Years);

public record HistoryYearEntry(HistoryKind Kind, string Description, string Year, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    public IList<string>? DotnetReleases { get; set; }
};



[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<HistoryKind>))]
[Description("The kind of history resource, indicating the chronological level of the index.")]
public enum HistoryKind
{
    HistoryIndex,
    HistoryYearIndex,
    HistoryMonthIndex,
}

/*
{
    "kind": "history-index",
    "description": "History of .NET releases",
    "_links": {
        "self": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/history/index.json",
            "relative": "index.json",
            "title": "History Index",
            "type": "application/hal+json"
            }
        },
    "_embedded": {
        "entries": [
            {
                "year": "2025",
                "kind": "history-year-index",
                "_links": {
                    "self": {
                        "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/history/2025/index.json",
                        "relative": "2025/index.json",
                        "title": "2025 History Index",
                        "type": "application/hal+json"
                    }
                },
                "dotnet-releases": ["8.0", "9.0", "10.0"],
            },
            {
                "year": "2024",
                "kind": "history-year-index",
                "_links": {
                    "self": {
                        "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/history/2024/index.json",
                        "relative": "2024/index.json",
                        "title": "2024 History Index",
                        "type": "application/hal+json"
                    }
                },
                "dotnet-releases": ["6.0", "7.0", "8.0", "9.0"],
            }
        ]
    }
}
*/
