using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices.Marshalling;
using System.Text.Json;
using System.Text.Json.Nodes;
using DotnetRelease;

string baseDir = "/Users/rich/git/core/release-notes/9.0";
string releasesJsonPath = Path.Combine(baseDir, "releases.json");
Stream releasesJson = File.OpenRead(releasesJsonPath);

var releasesOverview = await ReleaseNotes.GetMajorRelease(releasesJson) ?? throw new();

foreach (var release in releasesOverview.Releases)
{
    bool isStable = IsStable(release.ReleaseVersion, out string name);
    var releaseOverview = new PatchReleaseOverview(releasesOverview.ChannelVersion, release);
    WriteJson(releaseOverview, baseDir);

}

static void WriteJson(PatchReleaseOverview release, string baseDir)
{
    var releaseJson = "release.json";
    string version = release.Release.ReleaseVersion;
    var previewFolder = version.Contains("-preview") ? "" : "preview";
    string jsonPath = Path.Combine(baseDir, previewFolder, version, releaseJson);
    var options = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };
    var json = JsonSerializer.Serialize(release, options);    
    // File.WriteAllText(jsonPath, json);

    Console.WriteLine($"Writing {jsonPath}");
}

static bool IsStable(string version, out string name)
{
    if (version.Contains("-preview"))
    {
        var index = version.IndexOf('-') + 1;
        name = version[index..].Replace(".", "");
        return false;
    }

    name = version;
    return true;
} 