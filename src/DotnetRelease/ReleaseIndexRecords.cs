using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("A set of product releases with high-level information, like latest version.")]
public record ReleaseIndexOverview(IList<ReleaseSummary> ReleasesIndex);

[Description("A release, including major.minor version, latest patches, and whether that patch version included security updates.")]
public record ReleaseSummary(string ChannelVersion, string LatestRelease, string LatestReleaseDate, bool Security, string LatestRuntime, string LatestSdk, string Product, SupportPhase SupportPhase, string EolDate, ReleaseType ReleaseType)
{
    [JsonPropertyName("releases.json")]
    public string? ReleasesJson { get; set; }

    [JsonPropertyName("supported-os.json")]
    public string? SupportedOsJson { get; set; }
};

[JsonConverter(typeof(SnakeCaseStringEnumConverter<SupportPhase>))]
[Description("The various support phases of a product.")]
public enum SupportPhase
{
    Preview,
    GoLive,
    Active,
    Maintenance,
    Eol
}

[JsonConverter(typeof(SnakeCaseStringEnumConverter<ReleaseType>))]
[Description("The various release types, offering different support lengths.")]
public enum ReleaseType
{
    LTS,
    STS,
}
