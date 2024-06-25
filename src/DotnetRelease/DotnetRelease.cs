using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class Releases
{
    public static Task<ReleaseOverview?> GetDotnetRelease(HttpClient client, string url) => client.GetFromJsonAsync<ReleaseOverview>(url, DotnetReleaseSerializerContext.Default.ReleaseOverview);
    public static ValueTask<ReleaseOverview?> GetDotnetReleaseLocal(string url) => JsonSerializer.DeserializeAsync<ReleaseOverview>(File.OpenRead(url), DotnetReleaseSerializerContext.Default.ReleaseOverview);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(ReleaseOverview))]
internal partial class DotnetReleaseSerializerContext : JsonSerializerContext
{
}
