using DotnetRelease;
var test = "https://gist.githubusercontent.com/richlander/c1ec7e0b7eccd8132813199f8c4bbddf/raw/ac948a55b558dda08a9338d50f7bc9d5f40ba130/test.json";
HttpClient client = new();
var releases = await ReleaseIndex.GetDotnetRelease(client, test);

if (releases is null)
{
    return;
}

foreach(var release in releases.ReleasesIndex)
{
    Console.WriteLine($"{release.ChannelVersion}; {release.ReleaseType}; {release.SupportPhase}; {release.SupportedOsJson}; {release.ReleasesJson}");
}
