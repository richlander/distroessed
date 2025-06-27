using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
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

var entries = new List<ResourceEntry>();

var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
List<string> versionFiles = ["index.json", "releases.json", "release-info.json"];

foreach (var majorVersionDir in Directory.EnumerateDirectories(root).OrderDescending(numericStringComparer))
{
    var releasesJson = Path.Combine(majorVersionDir, "releases.json");
    if (!File.Exists(releasesJson))
    {
        continue;
    }

    var resources = IndexHelpers.GetResourcesForFiles(majorVersionDir, versionFiles);
    await using var stream = File.OpenRead(releasesJson);
    var major = await ReleaseNotes.GetMajorRelease(stream);
    if (major?.ChannelVersion is null || resources.Count == 0) continue;
    var majorVersion = major.ChannelVersion;
    var primary = resources[0];
    var majorEntry = new ResourceEntry(major.ChannelVersion, primary.Value, IndexHelpers.GetKindForSource(primary.Value));
    if (resources.Count > 1)
    {
        resources.RemoveAt(0); // Remove the primary resource from the list
        majorEntry.Resources = resources;
    }
    entries.Add(majorEntry);

    List<ResourceEntry> patchReleases = [];

    foreach (var patchDir in Directory.EnumerateDirectories(majorVersionDir).OrderDescending(numericStringComparer))
    {
        var patchJson = Path.Combine(patchDir, "release.json");
        if (!File.Exists(patchJson))
        {
            // Console.WriteLine($"Patch releases file not found: {patchJson}");
            continue;
        }

        await using var patchStream = File.OpenRead(patchJson);
        var patch = await ReleaseNotes.GetPatchRelease(patchStream);
        if (patch?.ChannelVersion is null) continue;
        var entry = new ResourceEntry(patch.ChannelVersion, patchJson, IndexHelpers.GetKindForSource(patchJson));
        patchReleases.Add(entry);
        entry.Resources = resources;

    }

    // Write patch index.json for this version directory if any patch releases found
    if (patchReleases.Count > 0)
    {
        var patchIndexPath = Path.Combine(majorVersionDir, "index.json");
        await WriteIndexJson(patchIndexPath, new Resources(new ResourceEntry("self", $".NET {majorVersion} Release Index", ResourceKind.Index), patchReleases));
    }

    // Console.WriteLine($"Wrote {patchReleases.Count} entries to {dir}");
}

var indexPath = Path.Combine(root, "index.json");
await WriteIndexJson(indexPath, new Resources(new ResourceEntry("self", ".NET Release Index", ResourceKind.Index), entries));
return 0;

static async Task WriteIndexJson(string path, Resources resources)
{
    // var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
    // var sorted = entries.OrderByDescending(e => e.Version, numericStringComparer);
    await using var outStream = File.Create(path);
    var options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(new LowerCaseNamingPolicy())
        }
    };

    await JsonSerializer.SerializeAsync(outStream, resources, options);
    Console.WriteLine($"Wrote {resources.Entries.Count} entries to {path}");
}
