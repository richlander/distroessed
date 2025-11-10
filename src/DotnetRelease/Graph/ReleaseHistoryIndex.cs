using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

[Description("Provides chronological access to .NET releases organized by time periods (years → months → releases)")]
public record ReleaseHistoryIndex(
    [Description("Type of history index (release-history-index, history-year-index, history-month-index)")]
    HistoryKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [Description("Context-aware description of the time period")]
    string Description,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{

    [JsonPropertyName("_usage"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Usage information including glossary of terms and help links")]
    public UsageWithLinks? Usage { get; set; }

    [JsonPropertyName("_embedded"),
     Description("Embedded time-based navigation entries and release summaries")]
    public ReleaseHistoryIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
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
    [Description("Type of history index for this year")]
    HistoryKind Kind,
    [Description("Description of the year's releases")]
    string Description,
    [Description("Year identifier (e.g., '2025')")]
    string Year,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this year's content")]
    Dictionary<string, HalLink> Links)
{
    [Description("List of .NET version identifiers released during this year")]
    public IList<string>? DotnetReleases { get; set; }
};

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<HistoryKind>))]
[Description("Identifies the type of history index document")]
public enum HistoryKind
{
    [Description("Root chronological index")]
    ReleaseHistoryIndex,
    [Description("Year-specific index")]
    HistoryYearIndex,
    [Description("Month-specific index")]
    HistoryMonthIndex,
}

/*
{
    "kind": "history-index",
    "description": "History of .NET releases",
    "_links": {
        "self": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/archives/index.json",
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
                        "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/archives/2025/index.json",
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
                        "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/archives/2024/index.json",
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
