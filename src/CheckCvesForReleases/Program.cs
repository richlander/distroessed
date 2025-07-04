using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using DotnetRelease;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: CheckCvesForReleases <path>");
    return;
}

var path = args[0];
if (!File.Exists(path) && !Directory.Exists(path))
{
    Console.Error.WriteLine($"Path not found: {path}");
    return;
}

var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);


foreach (var dir in Directory.EnumerateDirectories(path).OrderByDescending(d => d, numericStringComparer))
{
    var releasesJson = Path.Combine(dir, "releases.json");
    if (!File.Exists(releasesJson))
    {
        continue;
    }

    Console.WriteLine($"Processing release: {dir}");
    await using var stream = File.OpenRead(releasesJson);
    var majorRelease = await ReleaseNotes.GetMajorRelease(stream);

    if (majorRelease is null)
    {
        Console.WriteLine($"Failed to read release from {releasesJson}");
        continue;
    }

    // Iterate through each patch release in the major release
    // and check for CVEs
    foreach (var patch in majorRelease.Releases)
    {
        var version = patch.ReleaseVersion;
        var isSecurity = patch.Security;
        var cveCount = patch.CveList?.Count ?? 0;

        if (isSecurity && cveCount is 0)
        {
            Console.WriteLine($"Patch release {version} in {releasesJson} reports security = {isSecurity} with no CVEs.");
        }
        else if (!isSecurity && cveCount > 0)
        {
            Console.WriteLine($"Patch release {version} in {releasesJson} reports security = {isSecurity} with `cve-list` containing {cveCount} CVEs.");
        }
    }

    foreach (var patchDir in Directory.EnumerateDirectories(dir).OrderByDescending(d => d, numericStringComparer))
    {
        var patchJson = Path.Combine(patchDir, "release.json");
        if (!File.Exists(patchJson))
        {
            continue;
        }

        await using var patchStream = File.OpenRead(patchJson);
        var patchReleaseOverview = await ReleaseNotes.GetPatchRelease(patchStream);

        if (patchReleaseOverview is null)
        {
            Console.WriteLine($"Failed to read patch release from {patchJson}");
            continue;
        }

        var patchRelease = patchReleaseOverview.Release;

        if (patchRelease is null)
        {
            Console.WriteLine($"No release information found in {patchJson}; patch.Release is null.");
            continue;
        }

        var version = patchReleaseOverview.Release.ReleaseVersion;
        var isSecurity = patchRelease.Security;
        var cveCount = patchRelease.CveList?.Count ?? 0;


        if (isSecurity && cveCount is 0)
        {
            Console.WriteLine($"Patch release {version} in {patchJson} reports security = {isSecurity} with no CVEs.");
        }
        else if (!isSecurity && cveCount > 0)
        {
            Console.WriteLine($"Patch release {version} in {patchJson} reports security = {isSecurity} with `cve-list` containing {cveCount} CVEs.");
        }
    }
}
