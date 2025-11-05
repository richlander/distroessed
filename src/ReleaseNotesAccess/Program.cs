using DotnetRelease;

// Create an HttpClient and ReleaseNotesGraph
using var client = new HttpClient();
var graph = new ReleaseNotesGraph(client);

Console.WriteLine("=== .NET Release Notes Graph Demo ===\n");
Console.WriteLine("This demo shows both Layer 2 (low-level) and Layer 3 (high-level) APIs\n");

// === LAYER 3: High-Level API (ReleasesSummary) ===
Console.WriteLine("=== Layer 3: High-Level API ===\n");

// 1. Get releases summary (replaces releases-index.json)
Console.WriteLine("1. Getting releases summary (first call - fetches data)...");
var summary = graph.GetReleasesSummary();
var sw = System.Diagnostics.Stopwatch.StartNew();
var allVersions = await summary.GetAllVersionsAsync();
sw.Stop();
Console.WriteLine($"   Found {allVersions.Count()} .NET versions (took {sw.ElapsedMilliseconds}ms)");

Console.WriteLine("   Calling GetSupportedVersionsAsync() (cached - should be instant)...");
sw.Restart();
var supported = await summary.GetSupportedVersionsAsync();
sw.Stop();
Console.WriteLine($"   Currently supported: {string.Join(", ", supported.Select(s => s.Version))} (took {sw.ElapsedMilliseconds}ms - cached!)");

var latestLts = await summary.GetLatestLtsAsync();
Console.WriteLine($"   Latest LTS: {latestLts?.Version} (EOL: {latestLts?.EolDate:yyyy-MM-dd}) (also cached)");
Console.WriteLine();

// 2. Navigate into .NET 8.0 (ReleaseNavigator)
Console.WriteLine("2. Navigating into .NET 8.0 (first call - fetches data)...");
var nav = graph.GetReleaseNavigator("8.0");
sw.Restart();
var patches = await nav.GetAllPatchesAsync();
sw.Stop();
Console.WriteLine($"   Found {patches.Count()} patch releases (took {sw.ElapsedMilliseconds}ms)");

Console.WriteLine("   Calling GetAllPatchesAsync() again (cached - should be instant)...");
sw.Restart();
var patches2 = await nav.GetAllPatchesAsync();
sw.Stop();
Console.WriteLine($"   Found {patches2.Count()} patch releases (took {sw.ElapsedMilliseconds}ms - cached!)");

var latestPatch = await nav.GetLatestPatchAsync();
Console.WriteLine($"   Latest: {latestPatch?.Version} (Released: {latestPatch?.ReleaseDate:yyyy-MM-dd})");

var securityPatches = await nav.GetSecurityPatchesAsync();
Console.WriteLine($"   Security patches (using cached data):");
foreach (var patch in securityPatches.Take(3))
{
    Console.WriteLine($"     - {patch.Version}: {patch.CveCount} CVE(s)");
}
Console.WriteLine();

// 3. Real-world workflow: Security updates since a specific version
Console.WriteLine("3. Security updates since 8.0.16 (real-world scenario)...");
var nav80 = graph.GetReleaseNavigator("8.0");
var allPatches80 = await nav80.GetAllPatchesAsync();

// Find patches newer than 8.0.16
var currentVersion = "8.0.16";
var newerPatches = allPatches80
    .TakeWhile(p => p.Version != currentVersion)
    .ToList();

Console.WriteLine($"   You are on: {currentVersion}");
Console.WriteLine($"   Newer patches available: {newerPatches.Count}");

var securityUpdates = newerPatches.Where(p => p.HasCves).ToList();
Console.WriteLine($"   Security updates since {currentVersion}: {securityUpdates.Count}");

foreach (var patch in securityUpdates)
{
    Console.WriteLine($"     - {patch.Version}: {patch.CveCount} CVE(s) fixed");
}
Console.WriteLine();

// 4. Combined workflow: All supported LTS versions with latest patches
Console.WriteLine("4. Supported LTS versions with latest patches...");
var ltsVersions = await summary.GetVersionsByTypeAsync(ReleaseType.LTS);
var supportedLts = ltsVersions.Where(v => v.IsSupported);

foreach (var version in supportedLts)
{
    var versionNav = summary.GetNavigator(version.Version);
    var latest = await versionNav.GetLatestPatchVersionAsync();
    var hasUpdates = await versionNav.HasSecurityUpdatesAsync();
    Console.WriteLine($"   {version.Version}: Latest={latest}, HasSecurityUpdates={hasUpdates}");
}
Console.WriteLine();

// === ARCHIVES API: CVE Workflows ===
Console.WriteLine("\n=== Archives API: CVE-Focused Workflows ===\n");

// 5. Get all CVEs across all years
Console.WriteLine("5. Getting all CVE IDs across all release history...");
var archives = graph.GetArchivesSummary();
sw.Restart();
var allCveIds = await archives.GetAllCveIdsAsync();
sw.Stop();
Console.WriteLine($"   Total CVEs in release history: {allCveIds.Count()} (took {sw.ElapsedMilliseconds}ms)");
Console.WriteLine($"   Sample CVEs: {string.Join(", ", allCveIds.Take(3))}");
Console.WriteLine();

// 6. Navigate into a specific year for CVE details
Console.WriteLine("6. Navigating into 2024 release history...");
var year2024 = archives.GetNavigator("2024");
sw.Restart();
var months2024 = await year2024.GetAllMonthsAsync();
sw.Stop();
Console.WriteLine($"   Found {months2024.Count()} months in 2024 (took {sw.ElapsedMilliseconds}ms)");

var monthsWithCves = await year2024.GetMonthsWithCvesAsync();
Console.WriteLine($"   Months with CVEs: {monthsWithCves.Count()}");

foreach (var month in monthsWithCves.Take(3))
{
    Console.WriteLine($"     - {month.YearMonth}: {month.CveCount} CVE(s), affecting {string.Join(", ", month.DotnetReleases ?? [])}");
}
Console.WriteLine();

// 7. Get full CVE records for a specific month
Console.WriteLine("7. Getting full CVE records for a specific month...");
var firstMonthWithCves = monthsWithCves.FirstOrDefault();
if (firstMonthWithCves is not null)
{
    sw.Restart();
    var cveRecords = await year2024.GetCveRecordsForMonthAsync(firstMonthWithCves.Month);
    sw.Stop();
    Console.WriteLine($"   Fetched full CVE records for {firstMonthWithCves.YearMonth} (took {sw.ElapsedMilliseconds}ms)");

    if (cveRecords is not null)
    {
        Console.WriteLine($"   Title: {cveRecords.Title}");
        Console.WriteLine($"   CVEs: {cveRecords.Cves.Count}");
        Console.WriteLine($"   Affected Products: {cveRecords.Products.Count}");
        Console.WriteLine($"   Affected Packages: {cveRecords.Packages.Count}");

        var firstCve = cveRecords.Cves.FirstOrDefault();
        if (firstCve is not null)
        {
            Console.WriteLine($"   First CVE: {firstCve.Id} - {firstCve.Problem} (Severity: {firstCve.Severity})");
        }
    }
}
Console.WriteLine();

// 8. Get CVE count for specific year
Console.WriteLine("8. CVE statistics by year...");
var years = await archives.GetAllYearsAsync();
foreach (var year in years.Take(3))
{
    var yearNav = archives.GetNavigator(year.Year);
    var cveCount = await yearNav.GetCveCountAsync();
    Console.WriteLine($"   {year.Year}: {cveCount} CVE(s), {year.ReleaseCount} .NET version(s) released");
}
Console.WriteLine();

// === LAYER 2: Low-Level API ===
Console.WriteLine("\n=== Layer 2: Low-Level API (for reference) ===\n");

// 9. Get manifest directly
Console.WriteLine("9. Fetching manifest for .NET 8.0 (Layer 2)...");
var manifest = await graph.GetManifestAsync("8.0");
if (manifest is not null)
{
    Console.WriteLine($"   Title: {manifest.Title}");
    Console.WriteLine($"   Version: {manifest.Version}");
    Console.WriteLine($"   Release Type: {manifest.Lifecycle?.ReleaseType}");
}
Console.WriteLine();

Console.WriteLine("=== Demo Complete ===");
