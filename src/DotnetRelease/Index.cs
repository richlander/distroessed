using System.Text.Json.Serialization;

namespace DotnetRelease;

public record ReleaseIndex
{
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

    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink> Links { get; init; } = new();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ReleaseKind
{
    Index,
    Releases,
    Unknown
}
