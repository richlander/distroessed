using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

/// <summary>
/// Usage structure that combines terminology definitions with related navigation links.
/// </summary>
[Description("Usage information containing term definitions and related navigation links")]
public record UsageWithLinks
{
    [JsonPropertyName("_links"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("HAL+JSON links for usage-related resources")]
    public Dictionary<string, HalLink>? Links { get; set; }
    
    [JsonPropertyName("glossary"),
     Description("Term definitions (key-value pairs where key is the term and value is the definition)")]
    public Dictionary<string, string> Glossary { get; set; } = new();
}

/// <summary>
/// Base interface for .NET release version indexes.
/// </summary>
public interface IReleaseVersionIndex
{
    ReleaseKind Kind { get; }
    string Title { get; }
    string Description { get; }
    Dictionary<string, HalLink> Links { get; }
    UsageWithLinks? Usage { get; set; }
    GenerationMetadata? Metadata { get; set; }
}

/// <summary>
/// Legacy type for backward compatibility - prefer MajorReleaseVersionIndex or PatchReleaseVersionIndex
/// </summary>
[Description("Legacy index type - use MajorReleaseVersionIndex or PatchReleaseVersionIndex instead")]
public record ReleaseVersionIndex(
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
    [JsonPropertyName("usage"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Usage information and term definitions")]
    public UsageWithLinks? Usage { get; set; }

    [JsonPropertyName("_embedded"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Embedded release entries for this index level")]
    public ReleaseVersionIndexEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Lifecycle information (GA date, EOL date, release type, phase)")]
    public Lifecycle? Lifecycle { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded release entries in a version index (legacy)")]
public record ReleaseVersionIndexEmbedded(
    [Description("List of release entries with version information and navigation links")]
    List<ReleaseVersionIndexEntry> Releases);

[Description("Individual release entry within a version index, containing version metadata and navigation links (legacy)")]
public record ReleaseVersionIndexEntry(
    [Description("Version identifier (e.g., '8.0' for major version, '8.0.1' for patch version)")]
    string Version,
    [Description("Type of release (index, major-release, patch-release, etc.)")]
    ReleaseKind Kind,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this release's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     JsonPropertyName("lifecycle"),
     Description("Lifecycle information (phase and release-date)")]
    public PatchLifecycle? Lifecycle { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     JsonPropertyName("cve-records"),
     Description("CVE IDs associated with this release")]
    public IReadOnlyList<string>? CveRecords { get; set; }
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseKind>))]
[Description("Identifies the type of release or index document")]
public enum ReleaseKind
{
    [Description("Version-based index document")]
    Index,
    [Description("Release metadata document")]
    Manifest,
    [Description("Major version content")]
    MajorRelease,
    [Description("Patch version content")]
    PatchRelease,
    [Description("SDK feature band content")]
    Band,
    [Description("General content document")]
    Content,
    [Description("Unspecified type")]
    Unknown
}

