using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseIndex
{
    public ReleaseKind Kind { get; init; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();

    [JsonPropertyName("_embedded")]
    public ReleaseIndexEmbedded Embedded { get; init; } = new();
}

public record ReleaseIndexEmbedded
{
    public List<ReleaseIndexEntry> Releases { get; init; } = new();
}

public record ReleaseIndexEntry
{
    public string Version { get; init; } = default!;

    public ReleaseKind Kind { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Support? Support { get; set; }

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseKind>))]
public enum ReleaseKind
{
    Index,
    Manifest,
    Releases,
    Release,
    Unknown
}

