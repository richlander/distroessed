using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class Releases
{
    public static Task<ReleaseOverview?> GetDotnetRelease(HttpClient client, string url) => client.GetFromJsonAsync<ReleaseOverview>(url, ReleaseSerializerContext.Default.ReleaseOverview);
    public static ValueTask<ReleaseOverview?> GetDotnetRelease(Stream stream) => JsonSerializer.DeserializeAsync<ReleaseOverview>(stream, ReleaseSerializerContext.Default.ReleaseOverview);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(ReleaseOverview))]
internal partial class ReleaseSerializerContext : JsonSerializerContext
{
}
