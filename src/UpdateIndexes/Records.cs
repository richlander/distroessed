using System.Text.Json.Serialization;

public record ReleaseIndex([property: JsonPropertyName("entries")] List<ReleaseIndexEntry> Entries);

public record ReleaseIndexEntry(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("path")] string Path);
