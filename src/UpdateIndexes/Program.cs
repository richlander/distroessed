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
    Console.Error.WriteLine("Usage: UpdateIndexes <input-directory> [output-directory]");
    Console.Error.WriteLine("       UpdateIndexes --test-llms-template");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes data to read");
    Console.Error.WriteLine("  output-directory: Directory to write generated index files (optional, defaults to input-directory)");
    Console.Error.WriteLine("  --test-llms-template: Run llms.txt template test");
    return 1;
}

// Check for test mode
if (args.Length == 1 && args[0] == "--test-llms-template")
{
    LlmsTxtGeneratorTest.RunTest();
    return 0;
}

var inputDir = args[0];
var outputDir = args.Length > 1 ? args[1] : inputDir;

if (!Directory.Exists(inputDir))
{
    Console.Error.WriteLine($"Input directory not found: {inputDir}");
    return 1;
}

// Create output directory if it doesn't exist and it's different from input
if (inputDir != outputDir && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
    Console.WriteLine($"Created output directory: {outputDir}");
}

Console.WriteLine($"Input directory: {inputDir}");
if (inputDir != outputDir)
{
    Console.WriteLine($"Output directory: {outputDir}");
}

// Reset skipped files counters
ReleaseIndexFiles.ResetSkippedFilesCount();
HistoryIndexFiles.ResetSkippedFilesCount();

// Generate general release summaries
// This will read all the major version directories and their patch releases
// and produce a summary of the releases, including SDK bands and patch releases.
var summaries = await Summary.GetReleaseSummariesAsync(inputDir) ?? throw new InvalidOperationException("Failed to generate release summaries.");
ReleaseHistory history = Summary.GetReleaseCalendar(summaries);
Summary.PopulateCveInformation(history, inputDir);
await ReleaseIndexFiles.GenerateAsync(summaries, inputDir, outputDir, history);
await SdkIndexFiles.GenerateAsync(summaries, outputDir);
await HistoryIndexFiles.GenerateAsync(inputDir, outputDir, history);

// Display skipped files count
var totalSkipped = ReleaseIndexFiles.SkippedFilesCount + HistoryIndexFiles.SkippedFilesCount;
Console.WriteLine($"Skipped {totalSkipped} files because they did not change.");

return 0;