using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetSupport;

public class SupportedOS
{
    public static Task<SupportMatrix?> GetSupportMatrix(HttpClient client, string url) => client.GetFromJsonAsync<SupportMatrix>(url, SupportMatrixSerializerContext.Default.SupportMatrix);
    public static ValueTask<SupportMatrix?> GetSupportMatrix(Stream stream) => JsonSerializer.DeserializeAsync<SupportMatrix>(stream, SupportMatrixSerializerContext.Default.SupportMatrix);

    public static IList<string> SimplifyWindowsVersions(IList<String> versions)
    {
        List<string> updated = [];
        int length = 7;
        Span<char> upper = stackalloc char[length];
        for (int i = 0; i < versions.Count; i++)
        {
            if (i + 1 < versions.Count)
            {
                if (versions[i + 1].AsSpan().StartsWith(versions[i].AsSpan(0, length)))
                {
                    versions[i].AsSpan(0, length).ToUpperInvariant(upper);
                    updated.Add(upper.ToString());
                    i++;
                    continue;
                }
            }

            updated.Add(versions[i].ToUpperInvariant());
        }

        return updated;
    } 
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(SupportMatrix))]
internal partial class SupportMatrixSerializerContext : JsonSerializerContext
{
}
