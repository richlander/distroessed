using System.ComponentModel;
using System.Text.Json.Serialization;
using DotnetRelease.Security;

namespace DotnetRelease.Graph;

/// <summary>
/// Detailed index for a specific patch release (e.g., 9.0.0) containing CVE disclosures
/// </summary>
[Description("Detailed index for a specific patch release with CVE disclosure information")]
public record PatchDetailIndex(
    [Description("Type of release document, always 'patch-index' for patch detail indexes")]
    ReleaseKind Kind,
    [Description("Patch version identifier (e.g., '8.0.1', '9.0.2')")]
    string Version,
    [Description("Concise title for the document")]
    string Title,
    [Description("Description of the patch release")]
    string Description,
    [property: JsonPropertyName("_links"),
     Description("HAL+JSON links for hypermedia navigation")]
    Dictionary<string, HalLink> Links)
{
    [JsonPropertyName("lifecycle"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Simplified lifecycle information (phase and release-date only)")]
    public PatchLifecycle? Lifecycle { get; set; }

    [JsonPropertyName("disclosures"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("CVE security vulnerability disclosures for this patch release")]
    public IReadOnlyList<CveRecordSummary>? Disclosures { get; set; }

    [property: JsonPropertyName("_metadata"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Metadata about when and how this document was generated")]
    public GenerationMetadata? Metadata { get; set; }
}
