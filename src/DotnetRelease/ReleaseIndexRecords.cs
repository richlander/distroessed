using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[Description("A set of product releases with high-level information, like latest version.")]
public record ReleaseIndexOverview(IList<ReleaseSummary> ReleasesIndex);

[Description("A major.minor version release, including atest patches, and whether that patch version included security updates.")]
public record ReleaseSummary(
    [property: Description("Major (or major.minor) version of product.")]
    string ChannelVersion,

    [property: Description("The most recent patch release for the channel-version.")]
    string LatestRelease,
    
    [property: Description("The most recent release date for the channel-version.")]
    string LatestReleaseDate, bool Security,
    
    [property: Description("The most recent runtime patch version for the channel-version.")]
    string LatestRuntime,

    [property: Description("The most recent SDK patch version for the channel-version.")]
    string LatestSdk,
    
    [property: Description("The product name.")]
    string Product,
    
    [property: Description("The current support phase for the channel-version.")]
    SupportPhase SupportPhase,

    [property: Description("End of life date of .NET version.")]
    string EolDate,
    
    [property: Description("The release type for a .NET version.")]
    ReleaseType ReleaseType)
{
    [JsonPropertyName("releases.json")]
    public string? ReleasesJson { get; set; }

    [JsonPropertyName("supported-os.json")]
    public string? SupportedOsJson { get; set; }
};

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The various support phases of a product.")]
public enum SupportPhase
{
    Preview,
    GoLive,
    Active,
    Maintenance,
    Eol
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseType>))]
[Description("The various release types, offering different support lengths.")]
public enum ReleaseType
{
    LTS,
    STS,
}
