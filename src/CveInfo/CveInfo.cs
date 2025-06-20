using System.Text.Json;
using System.Text.Json.Serialization;

namespace CveInfo;

public class CveSerializer
{
    public static ValueTask<CveRecords?> GetCveRecords(Stream json) => JsonSerializer.DeserializeAsync(json, CveInfoSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(CveRecords))]
public partial class CveInfoSerializerContext : JsonSerializerContext
{
}
