using System.Text.Json;
using System.Text.Json.Serialization;

namespace CveInfo;

public class CveUtils
{
    public static ValueTask<CveSet?> GetCves(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveSet);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CveSet))]
internal partial class CveSerializerContext : JsonSerializerContext
{
}
