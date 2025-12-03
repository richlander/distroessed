using System.Text.Json.Serialization;

namespace BreakingChangesIndex;

/// <summary>
/// Root object for the breaking changes JSON file.
/// </summary>
public record BreakingChangesDocument(
    [property: JsonPropertyName("$schema")] string? Schema,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("breaking_change_count")] int BreakingChangeCount)
{
    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; init; }

    [JsonPropertyName("breaks")]
    public IReadOnlyList<BreakingChange>? Breaks { get; init; }

    [JsonPropertyName("categories")]
    public IReadOnlyDictionary<string, IReadOnlyList<string>>? Categories { get; init; }

    [JsonPropertyName("type_breakdown")]
    public IReadOnlyDictionary<string, int>? TypeBreakdown { get; init; }

    [JsonPropertyName("impact_breakdown")]
    public IReadOnlyDictionary<string, int>? ImpactBreakdown { get; init; }

    [JsonPropertyName("_metadata")]
    public BreakingChangesMetadata? Metadata { get; init; }
}

/// <summary>
/// A single breaking change entry.
/// </summary>
public record BreakingChange(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("category")] string Category)
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("version_introduced")]
    public string? VersionIntroduced { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("impact")]
    public string? Impact { get; init; }

    [JsonPropertyName("required_action")]
    public string? RequiredAction { get; init; }

    [JsonPropertyName("references")]
    public IReadOnlyList<BreakingChangeReference>? References { get; init; }

    [JsonPropertyName("affected_apis")]
    public IReadOnlyList<string>? AffectedApis { get; init; }
}

/// <summary>
/// A reference link for a breaking change.
/// </summary>
public record BreakingChangeReference(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("url")] string Url)
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

/// <summary>
/// Metadata about the breaking changes document generation.
/// </summary>
public record BreakingChangesMetadata(
    [property: JsonPropertyName("schema_version")] string SchemaVersion,
    [property: JsonPropertyName("generated_on")] DateTimeOffset GeneratedOn,
    [property: JsonPropertyName("generated_by")] string GeneratedBy)
{
    [JsonPropertyName("source_repository")]
    public string? SourceRepository { get; init; }

    [JsonPropertyName("source_path")]
    public string? SourcePath { get; init; }
}

/// <summary>
/// Represents an item from the toc.yml file.
/// </summary>
public class TocItem
{
    public string? Name { get; set; }
    public string? Href { get; set; }
    public List<TocItem>? Items { get; set; }
}
