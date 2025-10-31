using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class CveSerializer
{
    public static ValueTask<CveRecords?> GetCveRecords(Stream json) => JsonSerializer.DeserializeAsync(json, CveInfoSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(CveRecords))]
[JsonSerializable(typeof(CveRecord))]
[JsonSerializable(typeof(CvssInfo))]
[JsonSerializable(typeof(ProductEntry))]
[JsonSerializable(typeof(ExtensionEntry))]
[JsonSerializable(typeof(Commit))]
public partial class CveInfoSerializerContext : JsonSerializerContext
{
}
