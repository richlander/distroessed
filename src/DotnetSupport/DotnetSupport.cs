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
        for (int i = 0; i < versions.Count; i++)
        {
            var version = versions[i];
            if (i + 1 < versions.Count)
            {
                if (version.Contains("-e") && versions[i + 1].Contains("-w") &&
                    version.AsSpan().StartsWith(versions[i + 1].AsSpan(0, length)))
                {
                    version = version.AsSpan(0, length).ToString();
                    i++;
                }
            }

            string prettyVersion = PrettyifyWindowsVersion(version);
            updated.Add(prettyVersion);
        }

        return updated;
    }

    public static string PrettyifyWindowsVersion(string version)
    {
        // This is calling for a parser
        
        version = version.Replace('-', ' ').ToUpperInvariant();

        if (version.Length is 7)
        {
            return version;
        }
        else if (version.Contains('W'))
        {
            return $"{version.AsSpan(0, 7)} (W)";
        }
        else if (version.Contains('E'))
        {
            return $"{version.AsSpan(0, 7)} (E)";
        }
        else if (version.Contains("IOT"))
        {
            return $"{version.AsSpan(0, 7)} (IoT)";
        }

        return version;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
[JsonSerializable(typeof(SupportMatrix))]
internal partial class SupportMatrixSerializerContext : JsonSerializerContext
{
}
