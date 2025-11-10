using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

/// <summary>
/// Provides an index of patch .NET releases within a major version (e.g., 8.0.1, 8.0.2).
/// Uses simplified lifecycle information with only phase and release-date.
/// </summary>
[Description("Index of patch .NET releases with simplified lifecycle information")]
public record PatchReleaseVersionIndex(
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
     Description("Embedded patch release entries")]
    public PatchReleaseVersionIndexEmbedded? Embedded { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Lifecycle information (GA date, EOL date, release type, phase) for the major version")]
    public Lifecycle? Lifecycle { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}

[Description("Container for embedded patch release entries in a patch release index")]
public record PatchReleaseVersionIndexEmbedded(
    [Description("List of patch release entries with simplified lifecycle information")]
    List<PatchReleaseVersionIndexEntry> Releases);

[Description("Patch release entry within a major version index, containing simplified lifecycle information")]
public record PatchReleaseVersionIndexEntry(
    [Description("Patch version identifier (e.g., '8.0.1', '9.0.2')")]
    string Version,
    [Description("Type of release (patch-release)")]
    ReleaseKind Kind,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for navigation to this patch release's content")]
    Dictionary<string, HalLink> Links)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     JsonPropertyName("lifecycle"),
     Description("Simplified lifecycle information (phase and release-date only)")]
    public PatchLifecycle? Lifecycle { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability records associated with this release")]
    public IReadOnlyList<CveRecordSummary>? CveRecords { get; set; }
}
