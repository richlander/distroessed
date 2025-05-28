using System.Text.Json;
using System.Text.Json.Serialization;

namespace CveInfo;

public class CveSerializer
{
    public static ValueTask<CveRecords?> GetCveRecords(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(CveRecords))]
internal partial class CveSerializerContext : JsonSerializerContext
{
}
