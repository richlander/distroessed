using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease.Security;

public class CveUtils
{
    public static ValueTask<CveRecords?> GetCves(Stream json) => JsonSerializer.DeserializeAsync(json, CveSerializerContext.Default.CveRecords);
}


[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(CveRecords))]
[JsonSerializable(typeof(Event))]
[JsonSerializable(typeof(IList<Event>))]
[JsonSerializable(typeof(List<Event>))]
[JsonSerializable(typeof(CnaFaq))]
[JsonSerializable(typeof(IList<CnaFaq>))]
[JsonSerializable(typeof(List<CnaFaq>))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(IList<string>))]
public partial class CveSerializerContext : JsonSerializerContext
{
}
