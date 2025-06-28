using System.Globalization;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetRelease;
using UpdateIndexes;

const string index = "index.json";

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

List<string> files = ["index.json", "releases.json", "release.json", "manifest.json"];

var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
// Files to probe for to include as links

// List of major version entries
List<ReleaseIndexEntry> majorEntries = [];

// look at all the major version directories
foreach (var majorVersionDir in Directory.EnumerateDirectories(root).OrderDescending(numericStringComparer))
{
    var releasesJson = Path.Combine(majorVersionDir, "releases.json");
    if (!File.Exists(releasesJson))
    {
        Console.WriteLine($"Releases file not found: {releasesJson}");
        continue;
    }

    List<string> versionFiles = [.. IndexHelpers.GetQualifiedPaths(majorVersionDir, files)];

    Console.WriteLine($"Processing major version directory: {majorVersionDir}");

    await using var stream = File.OpenRead(releasesJson);
    var major = await ReleaseNotes.GetMajorRelease(stream);
    if (major?.ChannelVersion is null) continue;
    var majorVersion = major.ChannelVersion;
    // var patchLinks = IndexHelpers.GetIndexEntriesForFiles(root, "index.json", versionFiles, majorVersion);
    // if (patchLinks.Count == 0) continue;

    /*
    Example index.json, for root index
    {
      "_links": {
        "self": {
          "href": "https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json",
          "relative": "releases-index.json",
          "title": ".NET Release Index",
          "type": "application/json"
        }
      },
      "_embedded": {
        "releases": [
          {
            "version": "10.0",
            "kind": "releases",
            "_links": {
              "self": {
                "href": "https://.../10.0/releases.json",
                "relative": "10.0/releases.json",
                "type": "application/json"
              }
            }
          },
        ]
      }
    }

    */

    // var majorEntry = new ReleaseIndexEntry()
    // {
    //     Version = majorVersion,
    //     Kind = patchLinks.GetAt(0).Value.Kind ?? ReleaseKind.Unknown,
    //     Links = patchLinks
    // };

    // List of patch version entries
    List<ReleaseIndexEntry> patchEntries = [];

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

        var patchVersion = patch.ChannelVersion;

        // var patchEntry = new ReleaseIndexEntry()
        // {
        //     Version = patchVersion,
        //     Links = patchLinks
        // };

        var patchEntry = IndexHelpers.GetReleaseIndexEntry(
            patchDir,
            $"{patchVersion}",
            "index.json",
            files);
        patchEntries.Add(patchEntry);
    }

    var patchLinks = IndexHelpers.GetIndexEntriesForFiles(
        root,
        "index.json",
        versionFiles,
        majorVersion);

    // Write patch index.json for this version directory if any patch releases found
    if (patchEntries.Count > 0)
    {
        var patchIndexPath = Path.Combine(majorVersionDir, index);
        var patchIndex = new ReleaseIndex()
        {
            Kind = ReleaseKind.Index,
            Links = patchLinks,
            Embedded = new ReleaseIndexEmbedded()
            {
                Releases = patchEntries
            }
        };
        await WriteIndexJson(patchIndexPath, patchIndex);
    }

    // Console.WriteLine($"Wrote {patchEntries.Count} entries to {dir}");


    var majorEntry = IndexHelpers.GetReleaseIndexEntry(
        root,
        majorVersion,
        $".NET {majorVersion}",
        files);
    majorEntries.Add(majorEntry);
    if (major.EolDate > DateOnly.ParseExact("2016-01-01", "yyyy-MM-dd"))
    {
        // If EOL date is not set, skip
        majorEntry.Support = new Support(
            major.ReleaseType,
            major.SupportPhase,
            major.EolDate);
    }
}

var (majorIndexKey, majorIndexEntry) = IndexHelpers.GetIndexEntriesForFile(root, index, true, false, ".NET Releases");

if (majorIndexKey is null || majorIndexEntry is null)
{
    Console.Error.WriteLine($"No valid major version entries found in {root}");
    return 1;
}

var majorLinks = new Dictionary<string, HalLink>
{
    {majorIndexKey,majorIndexEntry}
};

var indexPath = Path.Combine(root, index);
var majorIndex = new ReleaseIndex()
{
    Kind = ReleaseKind.Index,
    Links = majorLinks,
    Embedded = new ReleaseIndexEmbedded()
    {
        Releases = majorEntries
    }
};
await WriteIndexJson(indexPath, majorIndex);
return 0;

static async Task WriteIndexJson(string path, ReleaseIndex index)
{
    await using var outStream = File.Create(path);
    await JsonSerializer.SerializeAsync(outStream, index, ReleaseIndexSerializerContext.Default.ReleaseIndex);
    Console.WriteLine($"Wrote {index.Embedded.Releases.Count} entries to {path}");
}
