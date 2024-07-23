using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

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
