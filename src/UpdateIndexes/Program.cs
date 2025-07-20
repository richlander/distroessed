using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using UpdateIndexes;

// Process all major version directories in the specified root directory
// Determine SDK feature bands from the releases.json files (complete view)
// Determine the composition of each patch version from release.json files
// Determine set of CVEs for each patch version; ensure it matches the CVE-specific data


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

// Generate general release summaries
// This will read all the major version directories and their patch releases
// and produce a summary of the releases, including SDK bands and patch releases.
var summaries = await Summary.GetReleaseSummariesAsync(root) ?? throw new InvalidOperationException("Failed to generate release summaries.");
ReleaseHistory history = Summary.GetReleaseCalendar(summaries);
Summary.PopulateCveInformation(history, root);
await ReleaseIndexFiles.GenerateAsync(summaries, root, history);
await SdkIndexFiles.GenerateAsync(summaries, root);
await HistoryIndexFiles.GenerateAsync(root, history);

return 0;