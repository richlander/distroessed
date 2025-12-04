using System.Text.Json.Serialization;

namespace BreakingChangesIndex;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(BreakingChangesDocument))]
internal partial class BreakingChangesSerializerContext : JsonSerializerContext
{
}
