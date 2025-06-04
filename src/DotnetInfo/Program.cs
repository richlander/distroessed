using DotnetInfo;

HttpClient httpClient = new();
DotnetReleaseInfo dotnetReleaseInfo = new(httpClient);

Console.WriteLine("Fetching Major Releases Index...");
Console.WriteLine("Printing Active Major Releases:");
foreach (var item in await dotnetReleaseInfo.GetActiveMajorReleaseIndexItemsAsync())
{
    Console.WriteLine($"Major Version: {item.ChannelVersion}");
    Console.WriteLine($"Latest Release: {item.LatestRelease}");
    Console.WriteLine($"Support Phase: {item.SupportPhase}");
    Console.WriteLine($"Patch Releases Info URI: {item.PatchReleasesInfoUri}");
    Console.WriteLine();
}

Console.WriteLine("Fetching Latest Patch Release Version for .NET 8...");
Console.WriteLine($"Latest Patch Release Version: {await dotnetReleaseInfo.GetLatestPatchReleaseVersionAsync("8.0")}");

Console.WriteLine("Fetching Patch Releases Since Version 8.0.8...");
var patchReleases = await dotnetReleaseInfo.GetPatchReleasesSinceVersionAsync("8.0", "8.0.8");
Console.WriteLine("Patch Releases Since 8.0.8:");
foreach (var release in patchReleases)
{
    Console.WriteLine($" - {release}");
}

Console.WriteLine("Fetching CVEs Since Version 8.0.8...");
var cves = await dotnetReleaseInfo.GetCvesSinceVersionAsync("8.0", "8.0.8");
Console.WriteLine("CVEs Since 8.0.8:");
foreach (var cve in cves)
{
    Console.WriteLine($" - {cve}");
}
Console.WriteLine("Done.");
