using System.Text.Json;
using System.Text.Json.Serialization;

namespace CveInfo;

public class CveUtils
{
    public static ValueTask<CveRecords?> GetCves(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CveRecords))]
internal partial class CveSerializerContext : JsonSerializerContext
{
}
