using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class ReleaseIndex
{
    public static Task<ReleaseIndexOverview?> GetDotnetRelease(HttpClient client, string url) => client.GetFromJsonAsync<ReleaseIndexOverview>(url, ReleaseIndexSerializerContext.Default.ReleaseIndexOverview);
    public static ValueTask<ReleaseIndexOverview?> GetDotnetRelease(Stream stream) => JsonSerializer.DeserializeAsync<ReleaseIndexOverview>(stream, ReleaseIndexSerializerContext.Default.ReleaseIndexOverview);

    public static string DefaultUrl { get; private set; } = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json";
}

[JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(ReleaseIndexOverview))]
internal partial class ReleaseIndexSerializerContext : JsonSerializerContext
{
}

public class SnakeCaseStringEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public SnakeCaseStringEnumConverter() : base(JsonNamingPolicy.KebabCaseLower)
    { }
}
