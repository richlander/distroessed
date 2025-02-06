using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;

namespace DotnetRelease;

public class ReleaseNotes
{
    // URLs
    public static string OfficialBaseUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/";

    public static string GitHubBaseUri { get; private set; } = "https://raw.githubusercontent.com/dotnet/core/main/release-notes/";

    public static string MajorReleasesIndexUri { get; private set; } = "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json";

    // File names
    public static string OSPackages { get; private set; } = "os-packages.json";

    public static string PatchReleasesIndex { get; private set; } = "patch-releases-index.json";

    public static string MajorReleasesIndex { get; private set; } = "releases-index.json";

    public static string Releases { get; private set; } = "releases.json";

    public static string PatchRelease { get; private set; } = "release.json";

    public static string SupportedOS { get; private set; } = "supported-os.json";

    public static string PreviewDirectory { get; private set; } = "preview";

    // Deserializer methods
    // Example file: https://github.com/dotnet/core/blob/main/release-notes/releases-index.json
    public static Task<MajorReleasesIndex?> GetMajorReleasesIndex(HttpClient client, string url) => client.GetFromJsonAsync<MajorReleasesIndex>(url, MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex);

    public static ValueTask<MajorReleasesIndex?> GetMajorReleasesIndex(Stream stream) => JsonSerializer.DeserializeAsync<MajorReleasesIndex>(stream, MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/releases.json
    public static Task<MajorReleaseOverview?> GetMajorRelease(HttpClient client, string url) => client.GetFromJsonAsync<MajorReleaseOverview>(url, MajorReleaseOverviewSerializerContext.Default.MajorReleaseOverview);

    public static ValueTask<MajorReleaseOverview?> GetMajorRelease(Stream stream) => JsonSerializer.DeserializeAsync<MajorReleaseOverview>(stream, MajorReleaseOverviewSerializerContext.Default.MajorReleaseOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/patch-releases-index.json
    public static Task<PatchReleasesIndex?> GetPatchReleasesIndex(HttpClient client, string url) => client.GetFromJsonAsync<PatchReleasesIndex>(url, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);

    public static ValueTask<PatchReleasesIndex?> GetPatchReleasesINdex(Stream stream) => JsonSerializer.DeserializeAsync<PatchReleasesIndex>(stream, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.1/release.json
    public static Task<PatchReleaseOverview?> GetPatchRelease(HttpClient client, string url) => client.GetFromJsonAsync<PatchReleaseOverview>(url, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);

    public static ValueTask<PatchReleaseOverview?> GetPatchRelease(Stream stream) => JsonSerializer.DeserializeAsync<PatchReleaseOverview>(stream, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/9.0/os-packages.json
    public static Task<OSPackagesOverview?> GetOSPackages(HttpClient client, string url) => client.GetFromJsonAsync<OSPackagesOverview>(url, OSPackagesSerializerContext.Default.OSPackagesOverview);

    public static ValueTask<OSPackagesOverview?> GetOSPackages(Stream stream) => JsonSerializer.DeserializeAsync<OSPackagesOverview>(stream, OSPackagesSerializerContext.Default.OSPackagesOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.json
    public static Task<SupportedOSMatrix?> GetSupportedOSes(HttpClient client, string url) => client.GetFromJsonAsync<SupportedOSMatrix>(url, SupportedOSMatrixSerializerContext.Default.SupportedOSMatrix);

    public static ValueTask<SupportedOSMatrix?> GetSupportedOSes(Stream stream) => JsonSerializer.DeserializeAsync<SupportedOSMatrix>(stream, SupportedOSMatrixSerializerContext.Default.SupportedOSMatrix);

    public static IEnumerable<DirectoryInfo> GetReleaseNoteDirectories(DirectoryInfo releaseNotesRoot) => releaseNotesRoot.EnumerateDirectories("*", SearchOption.TopDirectoryOnly).Where(d => decimal.TryParse(d.Name, out var num)).OrderByDescending(d => d.Name);

    public static ReleaseName GetReleaseName(string version)
    {
        int index = version.IndexOf('-');

        if (index < 0)
        {
            return new(version, version, false);
        }

        index++;
        string name = version[index..].Replace(".", "");

        if (version.StartsWith("9.0"))
        {
            return new(name, $"preview/{name}", true);
        }
        else
        {
            return new(name, "preview", true);
        }
    }

    public static string GetUri(string fileName, string version, string? baseUri = null)
    {
        baseUri ??= GitHubBaseUri;
        return baseUri.StartsWith("https")
            ? $"{baseUri}/{version}/{fileName}"
            : Path.Combine(baseUri, version, fileName);
    }

    public static string GetUri(string fileName, string? baseUri = null)
    {
        baseUri ??= GitHubBaseUri;
        return baseUri.StartsWith("https")
            ? $"{baseUri}/{fileName}"
            : Path.Combine(baseUri, fileName);
    }
}

public record ReleaseName(string Name, string Folder, bool IsPreview);
