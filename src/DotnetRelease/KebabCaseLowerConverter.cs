using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class KebabCaseLowerStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public KebabCaseLowerStringEnumConverter() : base(JsonNamingPolicy.KebabCaseLower)
    { }
}
