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


var numericStringComparer = StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.NumericOrdering);
// Files to probe for to include as links
List<string> versionFiles = ["index.json", "releases.json", "manifest.json"];

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


    Console.WriteLine($"Processing major version directory: {majorVersionDir}");
    var patchLinks = IndexHelpers.GetIndexEntriesForFiles(majorVersionDir, "index.json", versionFiles);

    await using var stream = File.OpenRead(releasesJson);
    var major = await ReleaseNotes.GetMajorRelease(stream);
    if (major?.ChannelVersion is null || patchLinks.Count == 0) continue;

    var majorVersion = major.ChannelVersion;
    var majorEntry = new ReleaseIndexEntry()
    {
        Version = majorVersion,
        Kind = ReleaseKind.Index,
        Links = patchLinks
    };

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

    majorEntries.Add(majorEntry);

    // List of patch version entries
    List<ReleaseIndexEntry> patchEntries = [];

    foreach (var patchDir in Directory.EnumerateDirectories(majorVersionDir).OrderDescending(numericStringComparer))
    {
        var patchJson = Path.Combine(patchDir, "release.json");
        if (!File.Exists(patchJson))
        {
            Console.WriteLine($"Patch releases file not found: {patchJson}");
            continue;
        }

        await using var patchStream = File.OpenRead(patchJson);
        var patch = await ReleaseNotes.GetPatchRelease(patchStream);
        if (patch?.ChannelVersion is null) continue;

        var patchVersion = patch.ChannelVersion;

        var patchEntry = new ReleaseIndexEntry()
        {
            Version = patchVersion,
            Kind = ReleaseKind.Index,
            Links = patchLinks
        };

        patchEntries.Add(patchEntry);
    }

    // Write patch index.json for this version directory if any patch releases found
    if (patchEntries.Count > 0)
    {
        var patchIndexPath = Path.Combine(majorVersionDir, index);
        var patchIndex = new ReleaseIndex()
        {
            Links = patchLinks,
            Embedded = new ReleaseIndexEmbedded()
            {
                Releases = patchEntries
            }
        };
        await WriteIndexJson(patchIndexPath, patchIndex);
    }

    // Console.WriteLine($"Wrote {patchEntries.Count} entries to {dir}");
}

var (majorKey, majorEntry) = IndexHelpers.GetIndexEntriesForFiles(root, index, true, false);

if (majorKey is null || majorEntry is null)
{
    Console.Error.WriteLine($"No valid major version entries found in {root}");
    return 1;
}

var majorLinks = new Dictionary<string, HalLink>
{
    {majorKey,majorEntry}
};

var indexPath = Path.Combine(root, index);
var majorIndex = new ReleaseIndex()
{
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
