using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class Releases
{
    public static Task<ReleasesOverview?> GetDotnetRelease(HttpClient client, string url) => client.GetFromJsonAsync<ReleasesOverview>(url, ReleaseSerializerContext.Default.ReleasesOverview);
    public static ValueTask<ReleasesOverview?> GetDotnetRelease(Stream stream) => JsonSerializer.DeserializeAsync<ReleasesOverview>(stream, ReleaseSerializerContext.Default.ReleasesOverview);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(ReleasesOverview))]
internal partial class ReleaseSerializerContext : JsonSerializerContext
{
}
