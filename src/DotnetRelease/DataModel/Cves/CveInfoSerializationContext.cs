using System.Text.Json;
using System.Text.Json.Serialization;
using CveInfo;

namespace DotnetRelease;

public class CveUtils
{
    public static ValueTask<CveRecords?> GetCves(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(CveRecords))]
public partial class CveSerializerContext : JsonSerializerContext
{
}
