using System.Text.Json.Serialization;

namespace DotnetRelease;

// A type like HalResource<T> would have been nice but that doesn't work.
// It would force something like a `T Data` property.
// Spec: https://datatracker.ietf.org/doc/html/draft-kelly-json-hal#name-hal-documents
// The HAL spec has all properties inline (except for the conttent within `_embedded`).
// It is more straightforward to model `T Embedded` however at the point that a custom
// type is needed, then why bother with a generic type at all (unless the same envelope can carry different data).
public record ReleaseVersionIndex(ReleaseKind Kind, string Description, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ReleaseVersionIndexEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Support? Support { get; set; }
}

public record ReleaseVersionIndexEmbedded(List<ReleaseVersionIndexEntry> Releases);

public record ReleaseVersionIndexEntry(string Version, ReleaseKind Kind, [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Support? Support { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CveRecordSummary>? CveRecords { get; set; }
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseKind>))]
public enum ReleaseKind
{
    Index,
    Manifest,
    MajorRelease,
    PatchRelease,
    Content,
    Unknown
}