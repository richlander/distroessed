namespace CveIndexMarkdown;

[System.Text.Json.Serialization.JsonSerializable(typeof(ReleaseHistory))]
[System.Text.Json.Serialization.JsonSerializable(typeof(ReleaseDays))]
[System.Text.Json.Serialization.JsonSerializable(typeof(Release))]
[System.Text.Json.Serialization.JsonSourceGenerationOptions(PropertyNamingPolicy = System.Text.Json.Serialization.JsonKnownNamingPolicy.KebabCaseLower, WriteIndented = true)]
internal partial class CveIndexMarkdownSerializationContext : System.Text.Json.Serialization.JsonSerializerContext
{
}