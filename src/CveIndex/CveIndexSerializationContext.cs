using System.Text.Json.Serialization;

namespace CveIndex;

[JsonSerializable(typeof(ReleaseCalendar))]
[JsonSerializable(typeof(ReleaseDay))]
[JsonSerializable(typeof(Release))]
[JsonSerializable(typeof(SevCount))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
public partial class CveIndexSerializationContext : JsonSerializerContext
{
}