using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace DotnetSupport;

public class SupportedOS
{
    private const string SupportMatrixJson = "https://raw.githubusercontent.com/dotnet/core/os-support/release-notes/9.0/supported-os.json";

    public static Task<SupportMatrix?> GetSupportMatrix(HttpClient client) => client.GetFromJsonAsync<SupportMatrix>(SupportMatrixJson, SupportMatrixSerializerContext.Default.SupportMatrix);
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(SupportMatrix))]
internal partial class SupportMatrixSerializerContext : JsonSerializerContext
{
}
