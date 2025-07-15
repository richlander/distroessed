using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseHistoryIndex(ReleaseHistoryKind Kind, string Description, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded")]
    public ReleaseHistoryIndexEmbedded? Embedded { get; set; }
}

public record ReleaseHistoryIndexEmbedded
{
    // public List<ReleaseHistoryYearEntry>? Years { get; set; }
    public List<ReleaseHistoryReleaseIndexEntry>? Releases { get; set; }
}

public record ReleaseHistoryReleaseIndexEntry(string Version, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links);

public record ReleaseHistoryYearIndexEmbedded(List<ReleaseHistoryYearEntry> Years);

public record ReleaseHistoryYearEntry(ReleaseHistoryKind Kind, string Description, string Year, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    public IList<string>? DotnetReleases { get; set; }
};



[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseHistoryKind>))]
public enum ReleaseHistoryKind
{
    ReleaseHistoryIndex,
    ReleaseHistoryYearIndex,
    ReleaseHistoryMonthIndex,
}

/*
{
    "kind": "release-history-index",
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
                "kind": "release-history-year-index",
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
                "kind": "release-history-year-index",
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