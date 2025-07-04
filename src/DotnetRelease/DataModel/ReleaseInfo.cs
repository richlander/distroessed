using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The support phases of a product.")]
public enum SupportPhase
{
    Preview,
    GoLive,
    Active,
    Maintenance,
    Eol
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseType>))]
[Description("The release types, offering different support lengths.")]
public enum ReleaseType
{
    LTS,
    STS,
}

public record Support(ReleaseType ReleaseType, SupportPhase phase, DateTimeOffset GaDate, DateTimeOffset EolDate);

public enum ProductComponent
{
    Runtime,
    SDK
}
