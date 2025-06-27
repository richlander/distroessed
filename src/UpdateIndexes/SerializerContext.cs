using System.Text.Json.Serialization;

namespace UpdateIndexes;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
[JsonSerializable(typeof(Resources))]
[JsonSerializable(typeof(ResourceEntry))]
public partial class ReleaseIndexSerializerContext : JsonSerializerContext {}
