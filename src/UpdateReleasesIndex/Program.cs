using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetRelease;

const string Preview = "preview";
string baseDir = "/Users/rich/git/core/release-notes";
string majorVersion = "9.0";
string releasesJsonPath = Path.Combine(baseDir, majorVersion, "releases.json");
string releaseFile = "release.json";
Stream releasesJson = File.OpenRead(releasesJsonPath);

var releasesOverview = await ReleaseNotes.GetMajorRelease(releasesJson) ?? throw new();

List<PatchReleaseIndexItem> patchReleases = [];

foreach (var release in releasesOverview.Releases)
{
    string version = release.ReleaseVersion;
    int previewIndex = version.IndexOf("-preview");
    bool isPreview = previewIndex > -1 ;
    previewIndex++;
    string previewFolder = isPreview ? $"/{Preview}" : "";
    string friendlyVersion = isPreview ?
        version[previewIndex..].Replace(".", "") :
        version;
    var releaseOverview = new PatchReleaseOverview(releasesOverview.ChannelVersion, release);
    var releaseJsonUrl = $"{ReleaseNotes.OfficialBaseUrl}/{majorVersion}{previewFolder}/{friendlyVersion}/{releaseFile}";
    var patchItem = new PatchReleaseIndexItem(version, release.ReleaseDate, release.Security, releaseJsonUrl);
    patchReleases.Add(patchItem);
    WriteReleaseJson(releaseOverview, baseDir, friendlyVersion, isPreview);
}

var index = new PatchReleasesIndex(releasesOverview.ChannelVersion, patchReleases);
WriteReleaseIndex(index, baseDir);

static void WriteReleaseJson(PatchReleaseOverview release, string baseDir, string version, bool isPreview = false)
{
    var releaseJson = "release.json";
    string path = Path.Combine(baseDir, release.ChannelVersion);
    string jsonPath = isPreview ?
        Path.Combine(path, "preview", version, releaseJson) :
        Path.Combine(path, version, releaseJson);
    var json = JsonSerializer.Serialize(release, PatchReleaseOverviewSerializerContext.Default.PatchReleaseOverview);    
    File.WriteAllText(jsonPath, json);
    Console.WriteLine($"Writing {jsonPath}");
}

static void WriteReleaseIndex(PatchReleasesIndex index, string baseDir)
{
    var json = JsonSerializer.Serialize(index, PatchReleasesIndexSerializerContext.Default.PatchReleasesIndex);
    var jsonPath = Path.Combine(baseDir, index.ChannelVersion, "patch-release-index.json");
    File.WriteAllText(jsonPath, json);
    Console.WriteLine($"Writing {jsonPath}");
}
