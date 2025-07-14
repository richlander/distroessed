using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

// A type like HalResource<T> would have been nice but that doesn't work.
// It would force something like a `T Data` property.
// Spec: https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#name-hal-documents
// The HAL spec has all properties inline (except for the conttent within `_embedded`).
// It is more straightforward to model `T Embedded` however at the point that a custom
// type is needed, then why bother with a generic type at all (unless the same envelope can carry different data).
[Description("Index of .NET releases, organized by major or patch versions with HAL+JSON hypermedia links.")]
public record ReleaseIndex(ReleaseKind Kind, string Description, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReleaseIndexEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Support? Support { get; set; }
}

[Description("Container for embedded release entries in a release index.")]
public record ReleaseIndexEmbedded(List<ReleaseIndexEntry> Releases);

[Description("Individual release entry with version information, links, and optional support/CVE data.")]
public record ReleaseIndexEntry(string Version, ReleaseKind Kind, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Support? Support { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CveRecordSummary>? CveRecords { get; set; }
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseKind>))]
[Description("The kind of release resource, indicating the type of content or index.")]
public enum ReleaseKind
{
    Index,
    Manifest,
    MajorRelease,
    PatchRelease,
    Content,
    Unknown
}

