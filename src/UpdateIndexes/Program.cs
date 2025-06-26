
using System;
using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using UpdateIndexes;

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: UpdateIndexes <directory>");
    return 1;
}

var root = args[0];
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"Directory not found: {root}");
    return 1;
}





var entries = new List<ReleaseIndexEntry>();


foreach (var dir in Directory.EnumerateDirectories(root))
{
    var releasesJson = Path.Combine(dir, "releases.json");
    if (!File.Exists(releasesJson))
    {
        continue;
    }

    await using var stream = File.OpenRead(releasesJson);
    var major = await ReleaseNotes.GetMajorRelease(stream);
    if (major?.ChannelVersion is null) continue;
    var relPath = Path.GetRelativePath(root, releasesJson);
    entries.Add(new ReleaseIndexEntry(major.ChannelVersion, relPath));

    List<ReleaseIndexEntry> patchReleases = [];

    foreach (var patchDir in Directory.EnumerateDirectories(dir))
    {
        var patchJson = Path.Combine(patchDir, "release.json");
        if (!File.Exists(patchJson))
        {
            continue;
        }

        await using var patchStream = File.OpenRead(patchJson);
        var patch = await ReleaseNotes.GetPatchRelease(patchStream);
        if (patch?.ChannelVersion is null) continue;
        var patchRelPath = Path.GetRelativePath(root, patchJson);
        patchReleases.Add(new ReleaseIndexEntry(patch.ChannelVersion, patchRelPath));
    }

    // Write patch index.json for this version directory if any patch releases found
    if (patchReleases.Count > 0)
    {
        var patchIndexPath = Path.Combine(dir, "index.json");
        await WriteIndexJson(patchIndexPath, patchReleases);
    }
}



var indexPath = Path.Combine(root, "index.json");
await WriteIndexJson(indexPath, entries);
return 0;

static async Task WriteIndexJson(string path, List<ReleaseIndexEntry> entries)
{
    var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
    var sorted = entries.OrderBy(e => e.Version, numericStringComparer).ToList();
    var index = new ReleaseIndex([..sorted]);
    await using var outStream = File.Create(path);
    await JsonSerializer.SerializeAsync(outStream, index, ReleaseIndexSerializerContext.Default.ReleaseIndex);
    Console.WriteLine($"Wrote {entries.Count} entries to {path}");
}
