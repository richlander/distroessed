using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

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
    string Description,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links) : IReleaseVersionIndex
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Glossary of .NET terminology and abbreviations")]
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
    string Version,
    [Description("Type of release (index for major version)")]
    ReleaseKind Kind,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this major version's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     JsonPropertyName("lifecycle"),
     Description("Full lifecycle information (release-type, phase, eol-date, supported)")]
    public Lifecycle? Lifecycle { get; set; }
}