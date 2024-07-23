using DotnetRelease;
HttpClient client = new();
var releases = await ReleaseNotes.GetMajorReleasesIndex(client, ReleaseNotes.MajorReleasesIndexUrl);

if (releases is null)
{
    return;
}

foreach(var release in releases.ReleasesIndex)
{
    Console.WriteLine($"{release.ChannelVersion}; {release.ReleaseType}; {release.SupportPhase}; {release.SupportedOsJson}; {release.ReleasesJson}");
}
