using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("Manifest containing release metadata including version, dates, and support information.")]
public record ReleaseManifest(
    ReleaseKind Kind,
    [property: JsonPropertyName("_links")] Dictionary<string, HalLink> Links,
    string Version,
    string Label,
    DateTimeOffset GaDate,
    DateTimeOffset EolDate,
    ReleaseType ReleaseType,
    SupportPhase SupportPhase)
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Runtime version information for this release.")]
    public RuntimeVersionInfo? Runtime { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("SDK version information for this release.")]
    public SdkVersionInfo? Sdk { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("CVE records associated with this release.")]
    public IReadOnlyList<CveRecordSummary>? CveRecords { get; set; }
}

[Description("Runtime version information for a .NET release.")]
public record RuntimeVersionInfo(
    [property: Description("The .NET runtime version.")]
    string Version,
    [property: Description("The release date of this runtime version.")]
    DateTimeOffset ReleaseDate)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Additional version details or build information.")]
    public string? BuildInfo { get; set; }
}

[Description("SDK version information for a .NET release.")]
public record SdkVersionInfo(
    [property: Description("The .NET SDK version.")]
    string Version,
    [property: Description("The release date of this SDK version.")]
    DateTimeOffset ReleaseDate)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Additional version details or build information.")]
    public string? BuildInfo { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [Description("Feature band information for the SDK.")]
    public string? FeatureBand { get; set; }
}

    