using DotnetRelease;

// Create an HttpClient and ReleaseNotesGraph
using var client = new HttpClient();
var graph = new ReleaseNotesGraph(client);

Console.WriteLine("=== .NET Release Notes Graph Demo ===\n");

// 1. Get the major release index
Console.WriteLine("1. Fetching major release index...");
var majorIndex = await graph.GetMajorReleaseIndexAsync();
if (majorIndex?.Embedded?.Releases is not null)
{
    Console.WriteLine($"   Found {majorIndex.Embedded.Releases.Count} .NET versions");

    // Show supported versions
    var supported = majorIndex.Embedded.Releases
        .Where(r => r.Lifecycle?.Supported == true)
        .ToList();

    Console.WriteLine($"   Currently supported: {string.Join(", ", supported.Select(s => s.Version))}");
}
Console.WriteLine();

// 2. Get patch releases for .NET 8.0
Console.WriteLine("2. Fetching patch releases for .NET 8.0...");
var patchIndex = await graph.GetPatchReleaseIndexAsync("8.0");
if (patchIndex?.Embedded?.Releases is not null)
{
    Console.WriteLine($"   Found {patchIndex.Embedded.Releases.Count} patch releases");
    var latestPatch = patchIndex.Embedded.Releases.First();
    Console.WriteLine($"   Latest: {latestPatch.Version} (Released: {latestPatch.Lifecycle?.ReleaseDate:yyyy-MM-dd})");

    // Show patches with CVEs
    var withCves = patchIndex.Embedded.Releases
        .Where(r => r.CveRecords?.Count > 0)
        .Take(3)
        .ToList();

    Console.WriteLine($"   Recent security patches:");
    foreach (var patch in withCves)
    {
        Console.WriteLine($"     - {patch.Version}: {patch.CveRecords?.Count} CVE(s)");
    }
}
Console.WriteLine();

// 3. Get manifest for .NET 8.0
Console.WriteLine("3. Fetching manifest for .NET 8.0...");
var manifest = await graph.GetManifestAsync("8.0");
if (manifest is not null)
{
    Console.WriteLine($"   Title: {manifest.Title}");
    Console.WriteLine($"   Version: {manifest.Version}");
    Console.WriteLine($"   Label: {manifest.Label}");
    Console.WriteLine($"   Release Type: {manifest.Lifecycle?.ReleaseType}");
    Console.WriteLine($"   Phase: {manifest.Lifecycle?.Phase}");
    Console.WriteLine($"   Release Date: {manifest.Lifecycle?.ReleaseDate:yyyy-MM-dd}");
    Console.WriteLine($"   EOL Date: {manifest.Lifecycle?.EolDate:yyyy-MM-dd}");
}
Console.WriteLine();

// 4. Get release history index
Console.WriteLine("4. Fetching release history index...");
var historyIndex = await graph.GetReleaseHistoryIndexAsync();
if (historyIndex?.Embedded?.Years is not null)
{
    Console.WriteLine($"   Found {historyIndex.Embedded.Years.Count} years of release history");
    var recentYears = historyIndex.Embedded.Years.Take(3).ToList();
    foreach (var year in recentYears)
    {
        Console.WriteLine($"   - {year.Year}: {year.DotnetReleases?.Count ?? 0} versions released");
    }
}
Console.WriteLine();

// 5. HAL Link Following Demo
Console.WriteLine("5. Demonstrating HAL link following...");
if (majorIndex?.Links is not null && majorIndex.Links.TryGetValue("latest-lts", out var ltsLink))
{
    Console.WriteLine($"   Following 'latest-lts' link: {ltsLink.Title}");
    var ltsIndex = await graph.FollowLinkAsync<DotnetRelease.Graph.PatchReleaseVersionIndex>(ltsLink);
    if (ltsIndex is not null)
    {
        Console.WriteLine($"   Successfully fetched: {ltsIndex.Title}");
        Console.WriteLine($"   Latest patch: {ltsIndex.Embedded?.Releases?.FirstOrDefault()?.Version}");
    }
}

Console.WriteLine("\n=== Demo Complete ===");
