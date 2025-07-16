using System.Text.Json;

namespace DotnetRelease;

public class ReleaseNotes
{
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
    public static ValueTask<MajorReleasesIndex?> GetMajorReleasesIndex(Stream stream) => JsonSerializer.DeserializeAsync<MajorReleasesIndex>(stream, MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/releases.json
    public static ValueTask<MajorReleaseOverview?> GetMajorRelease(Stream stream) => JsonSerializer.DeserializeAsync<MajorReleaseOverview>(stream, MajorReleaseOverviewSerializerContext.Default.MajorReleaseOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/patch-releases-index.json
    public static ValueTask<PatchReleasesIndex?> GetPatchReleasesIndex(Stream stream) => JsonSerializer.DeserializeAsync<PatchReleasesIndex>(stream, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.1/release.json
    public static ValueTask<PatchReleaseOverview?> GetPatchRelease(Stream stream) => JsonSerializer.DeserializeAsync<PatchReleaseOverview>(stream, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/9.0/os-packages.json
    public static ValueTask<OSPackagesOverview?> GetOSPackages(Stream stream) => JsonSerializer.DeserializeAsync<OSPackagesOverview>(stream, OSPackagesSerializerContext.Default.OSPackagesOverview);

    // Example file: https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.json
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
}

public record ReleaseName(string Name, string Folder, bool IsPreview);
