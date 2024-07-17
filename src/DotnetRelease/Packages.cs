using System.Net.Http.Json;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetRelease;

public class Packages
{
    public static Task<PackageOverview?> GetPackageOverview(HttpClient client, string url) => client.GetFromJsonAsync<PackageOverview>(url, PackageSerializerContext.Default.PackageOverview);
    public static ValueTask<PackageOverview?> GetPackageOverview(Stream stream) => JsonSerializer.DeserializeAsync<PackageOverview>(stream, PackageSerializerContext.Default.PackageOverview);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(PackageOverview))]
internal partial class PackageSerializerContext : JsonSerializerContext
{
}
