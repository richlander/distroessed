using System.Text.Json;
using System.Text.Json.Serialization;

namespace CveInfo;

public class Cves
{
    public static ValueTask<CveSet?> GetCves(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveSet);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(CveSet))]
internal partial class CveSerializerContext : JsonSerializerContext
{
}
