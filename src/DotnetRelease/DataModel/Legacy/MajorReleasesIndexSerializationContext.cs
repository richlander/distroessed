using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleasesIndex))]
public partial class MajorReleasesIndexSerializerContext : JsonSerializerContext
{
}
