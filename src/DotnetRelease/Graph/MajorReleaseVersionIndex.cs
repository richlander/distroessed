using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// Provides an index of major .NET releases (root index containing major versions like 8.0, 9.0).
/// Uses full lifecycle information with release-type, eol-date, and supported fields.
/// </summary>
[Description("Index of major .NET releases with full lifecycle information")]
public record MajorReleaseVersionIndex(
    [Description("Type of release document, always 'index' for version-based indexes")]
    ReleaseKind Kind,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the index scope")]
    string Description)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest stable .NET version")]
    public string? Latest { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest LTS (Long-Term Support) .NET version")]
    public string? LatestLts { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Latest year with .NET releases (cross-reference to timeline)")]
    public string? LatestYear { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    public Dictionary<string, HalLink> Links { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Usage links to documentation and help resources")]
    public UsageLinks? Usage { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Glossary of terms and definitions")]
    public Dictionary<string, string>? Glossary { get; set; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded major version entries")]
    public MajorReleaseVersionIndexEmbedded? Embedded { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded major version entries in a major release index")]
public record MajorReleaseVersionIndexEmbedded(
    [Description("List of major version entries with full lifecycle information")]
    List<MajorReleaseVersionIndexEntry> Releases);

[Description("Major version entry within the root index, containing full lifecycle information")]
public record MajorReleaseVersionIndexEntry(
    [Description("Major version identifier (e.g., '8.0', '9.0')")]
    string Version)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Release type: lts (Long-Term Support) or sts (Standard-Term Support)")]
    public ReleaseType? ReleaseType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Current support phase (preview, go-live, active, maintenance, eol)")]
    public SupportPhase? Phase { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Whether this version is currently supported")]
    public bool? Supported { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("True if this release includes security fixes")]
    public bool? Security { get; init; }

    [Description("Number of CVEs affecting this release")]
    public int CveCount { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("General Availability date when this version was released")]
    public DateTimeOffset? GaDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("End of Life date when support ends")]
    public DateTimeOffset? EolDate { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE identifiers affecting this release")]
    public IList<string>? CveRecords { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Runtime patch versions for this major version released in the period")]
    public IList<string>? RuntimesPatches { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("SDK patch versions for this major version released in the period")]
    public IList<string>? SdkPatches { get; init; }

    [JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this major version's content")]
    public Dictionary<string, HalLink> Links { get; init; } = [];
}