using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The support phases of a .NET release through its lifecycle")]
public enum SupportPhase
{
    [Description("Pre-release phase with previews and release candidates")]
    Preview,
    [Description("Production-ready but with limited support")]
    GoLive,
    [Description("Full support with regular updates and security fixes")]
    Active,
    [Description("Security updates only, no new features")]
    Maintenance,
    [Description("End of life, no further updates")]
    Eol
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseType>))]
[Description("The release support models offering different support lengths")]
public enum ReleaseType
{
    [Description("Long Term Support - 3 years of support")]
    LTS,
    [Description("Standard Term Support - 18 months of support")]
    STS,
}

[Description("Support lifecycle information for a .NET release")]
public record Support(
    [Description("Support model (LTS or STS)")]
    ReleaseType ReleaseType,
    [Description("Current lifecycle phase")]
    SupportPhase phase,
    [Description("General Availability date when the release became stable")]
    DateTimeOffset GaDate,
    [Description("End of Life date when support ends")]
    DateTimeOffset EolDate);

public enum ProductComponent
{
    Runtime,
    SDK
}
