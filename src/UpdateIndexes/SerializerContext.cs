using System.Text.Json.Serialization;

namespace UpdateIndexes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(ReleaseIndex))]
[JsonSerializable(typeof(ReleaseIndexEntry))]
public partial class ReleaseIndexSerializerContext : JsonSerializerContext {}
