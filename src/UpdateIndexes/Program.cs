using System.Globalization;
using System.Text.Json;
using DotnetRelease;
using UpdateIndexes;

// Process all major version directories in the specified root directory
// Determine SDK feature bands from the releases.json files (complete view)
// Determine the composition of each patch version from release.json files
// Determine set of CVEs for each patch version; ensure it matches the CVE-specific data

if (args.Length == 0 || args.Length > 2)
{
    Console.Error.WriteLine("Usage: UpdateIndexes <input-directory> [output-directory]");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes to read from");
    Console.Error.WriteLine("  output-directory: Directory to write generated indexes to (optional)");
    Console.Error.WriteLine("                    If not specified, input-directory is used for both reading and writing");
    return 1;
}

var inputRoot = args[0];
var outputRoot = args.Length == 2 ? args[1] : inputRoot;

if (!Directory.Exists(inputRoot))
{
    Console.Error.WriteLine($"Input directory not found: {inputRoot}");
    return 1;
}

// Ensure output directory exists
if (!Directory.Exists(outputRoot))
{
    try
    {
        Directory.CreateDirectory(outputRoot);
        Console.WriteLine($"Created output directory: {outputRoot}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Failed to create output directory '{outputRoot}': {ex.Message}");
        return 1;
    }
}

// Display the directories being used
Console.WriteLine($"Input directory:  {inputRoot}");
Console.WriteLine($"Output directory: {outputRoot}");

// Reset skipped files counters
ReleaseIndexFiles.ResetSkippedFilesCount();
HistoryIndexFiles.ResetSkippedFilesCount();

// Generate general release summaries
// This will read all the major version directories and their patch releases
// and produce a summary of the releases, including SDK bands and patch releases.
var summaries = await Summary.GetReleaseSummariesAsync(inputRoot) ?? throw new InvalidOperationException("Failed to generate release summaries.");
ReleaseHistory history = Summary.GetReleaseCalendar(summaries);
Summary.PopulateCveInformation(history, inputRoot);
await ReleaseIndexFiles.GenerateAsync(summaries, outputRoot, history);
await HistoryIndexFiles.GenerateAsync(outputRoot, history);

// Display skipped files count
var totalSkipped = ReleaseIndexFiles.SkippedFilesCount + HistoryIndexFiles.SkippedFilesCount;
Console.WriteLine($"Skipped {totalSkipped} files because they did not change.");

return 0;