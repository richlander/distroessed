using System.Text.Json;
using DotnetRelease;
// HttpClient client = new();
// var uri = $"{ReleaseNotes.OfficialBaseUri}9.0/releases.json";
var releases = await ReleaseNotes.GetMajorRelease(File.OpenRead("/Users/rich/git/core/release-notes/9.0/releases.json"));

if (releases is null)
{
    return;
}

var json = JsonSerializer.Serialize(releases, MajorReleaseOverviewSerializerContext.Default.MajorReleaseOverview);
File.WriteAllText(ReleaseNotes.Releases, json);
