using System.Text.Json.Serialization;

namespace DotnetRelease.Index;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower,
    WriteIndented = true)]
[JsonSerializable(typeof(MajorReleasesIndex))]
public partial class MajorReleasesIndexSerializerContext : JsonSerializerContext
{
}
