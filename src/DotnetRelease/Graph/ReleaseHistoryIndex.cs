using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

[Description("Provides chronological access to .NET releases organized by time periods (years → months → releases)")]
public record ReleaseHistoryIndex(
    [Description("Type of timeline index (release-timeline-index, timeline-year-index, timeline-month-index)")]
    HistoryKind Kind,
    [Description("Concise title for the document")]
    string Title)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Context-aware description of the time period")]
    public string? Description { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest major .NET version (cross-reference to releases)")]
    public string? Latest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest LTS .NET version (cross-reference to releases)")]
    public string? LatestLts { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest year with .NET releases (primary)")]
    public string? LatestYear { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest month with security releases (format: YYYY-MM, e.g., '2025-10')")]
    public string? LatestSecurityMonth { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Glossary of timeline-specific terms and definitions")]
    public Dictionary<string, string>? Glossary { get; set; }

    [JsonPropertyName("_embedded"),
     Description("Embedded time-based navigation entries and release summaries")]
    public ReleaseHistoryIndexEmbedded? Embedded { get; set; }
}

[Description("Container for embedded chronological navigation entries")]
public record ReleaseHistoryIndexEmbedded
{
    [Description("Yearly navigation entries (root level history index)")]
    public List<HistoryYearEntry>? Years { get; set; }
}

[Description("Individual release entry within a history index, linking to version-specific content")]
public record ReleaseHistoryIndexEntry(
    [Description("Version identifier for the release")]
    string Version,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this release's content")]
    Dictionary<string, HalLink> Links);

[Description("Container for yearly history entries")]
public record YearIndexEmbedded(
    [Description("List of year entries with navigation links")]
    List<HistoryYearEntry> Years);

[Description("Year entry in the release history, containing annual release information")]
public record HistoryYearEntry(
    [Description("Year identifier (e.g., '2025')")]
    string Year)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Description of the year's releases")]
    public string? Description { get; init; }
    [Description("List of .NET version identifiers released during this year")]
    public IList<string>? Releases { get; set; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this year's content")]
    public Dictionary<string, HalLink> Links { get; set; } = [];
};

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<HistoryKind>))]
[Description("Identifies the type of timeline index document")]
public enum HistoryKind
{
    [Description("Root chronological index")]
    TimelineIndex,
    [Description("Year-specific index")]
    YearIndex,
    [Description("Month-specific index")]
    MonthIndex,
    [Description("Resource manifest for a timeline entry")]
    Manifest,

    // Legacy values (deprecated, for backwards compatibility)
    [Description("Legacy: Use TimelineIndex instead")]
    ReleaseTimelineIndex,
    [Description("Legacy: Use YearIndex instead")]
    TimelineYearIndex,
    [Description("Legacy: Use MonthIndex instead")]
    TimelineMonthIndex,
}

/*
{
    "kind": "history-index",
    "description": "History of .NET releases",
    "_links": {
        "self": {
            "href": "https://raw.githubusercontent.com/dotnet/core/main/release-notes/archives/index.json",
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
                        "href": "https://raw.githubusercontent.com/dotnet/core/main/release-notes/archives/2025/index.json",
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
                        "href": "https://raw.githubusercontent.com/dotnet/core/main/release-notes/archives/2024/index.json",
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
