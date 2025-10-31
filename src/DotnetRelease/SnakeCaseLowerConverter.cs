using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class SnakeCaseLowerStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public SnakeCaseLowerStringEnumConverter() : base(JsonNamingPolicy.SnakeCaseLower)
    { }
}
