using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetRelease;

const string majorReleaseFile = "releases.json";
const string patchReleaseFile = "release.json";
string baseDir = "/Users/rich/git/core/release-notes";

List<MajorReleaseIndexItem> majorReleases = [];

foreach (var versionDir in ReleaseNotes.GetReleaseNoteDirectories(new DirectoryInfo(baseDir)))
{
    string releasesJsonPath = Path.Combine(versionDir.FullName, majorReleaseFile);
    Stream releasesJson = File.OpenRead(releasesJsonPath);
    var major = await ReleaseNotes.GetMajorRelease(releasesJson) ?? throw new();
    string releasesJsonUri = $"{ReleaseNotes.OfficialBaseUri}{major.ChannelVersion}/{majorReleaseFile}";

    var majorReleaseItem = new MajorReleaseIndexItem(
        major.ChannelVersion,
        major.LatestRelease,
        major.LatestReleaseDate,
        major.LatestReleaseSecurity,
        major.LatestRuntime,
        major.LatestSdk,
        major.Product,
        major.SupportPhase,
        major.EolDate,
        major.ReleaseType,
        releasesJsonUri,
        releasesJsonUri,
        major.PatchReleasesIndexUri,
        major.SupportedOsInfoUri,
        major.SupportedOsInfoUri,
        major.OsPackagesInfoUri);

    majorReleases.Add(majorReleaseItem);

    if (major.SupportPhase is SupportPhase.Eol)
    {
        continue;
    }

    ProcessMajorRelease(major, versionDir.FullName);
}

MajorReleasesIndex index = new(majorReleases);
string majorReleasesIndexJson = Path.Combine(baseDir, "releases-index.json");
WriteMajorReleasesIndex(index, majorReleasesIndexJson);

static void ProcessMajorRelease(MajorReleaseOverview majorReleaseOverview, string dir)
{
    List<PatchReleaseIndexItem> patchReleases = [];
    string channelVersion = majorReleaseOverview.ChannelVersion;
    int majorVersion = (int)decimal.Parse(channelVersion);

    foreach (var release in majorReleaseOverview.Releases)
    {
        var (name, folder, isPreview) = ReleaseNotes.GetReleaseName(release.ReleaseVersion);
        if (isPreview && majorVersion < 9)
        {
            continue;
        }
        var releaseJsonUrl = $"{ReleaseNotes.OfficialBaseUri}{channelVersion}/{folder}/{patchReleaseFile}";
        string patchReleaseJson = Path.Combine(dir, folder, "release.json");

        var patchReleaseOverview = new PatchReleaseOverview(
            channelVersion,
            release.ReleaseDate,
            release.ReleaseVersion,
            release.Security,
            release);

        var patchItem = new PatchReleaseIndexItem(
            release.ReleaseVersion,
            release.ReleaseDate,
            release.Security,
            releaseJsonUrl);

        patchReleases.Add(patchItem);

        WritePatchReleaseJson(patchReleaseOverview, patchReleaseJson);
    }

    var latestRelease = majorReleaseOverview.Releases[0];

    var index = new PatchReleasesIndex(
        channelVersion,
        latestRelease.ReleaseVersion,
        latestRelease.ReleaseDate,
        latestRelease.Security,
        majorReleaseOverview.SupportedOsInfoUri,
        majorReleaseOverview.OsPackagesInfoUri,
        patchReleases);

    string patchReleasesIndexJson = Path.Combine(dir, "patch-releases-index.json");
    WritePatchReleasesIndex(index, patchReleasesIndexJson);
}

static void WriteMajorReleasesIndex(MajorReleasesIndex index, string file)
{
    var json = JsonSerializer.Serialize(index, MajorReleasesIndexSerializerContext.Default.MajorReleasesIndex);
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}

static void WritePatchReleasesIndex(PatchReleasesIndex index, string file)
{
    var json = JsonSerializer.Serialize(index, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}

static void WritePatchReleaseJson(PatchReleaseOverview release, string file)
{
    var json = JsonSerializer.Serialize(release, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);    
    File.WriteAllText(file, json);
    Console.WriteLine($"Writing {file}");
}
