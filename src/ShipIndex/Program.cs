using DotnetRelease;
using ShipIndex;

// Generates .NET ship history index files (chronological: years -> months -> days)
// - Root archives/index.json with all years
// - Per-year archives/{year}/index.json files
// - Per-month archives/{year}/{month}/index.json files
// - CVE information linked to ship days

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ShipIndex <input-directory> [output-directory]");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes data to read");
    Console.Error.WriteLine("  output-directory: Directory to write generated index files (optional, defaults to input-directory)");
    return 1;
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

// Reset skipped files counter
ShipIndexFiles.ResetSkippedFilesCount();

// Generate release summaries and calendar from source data
var summaries = await Summary.GetReleaseSummariesAsync(inputDir) 
    ?? throw new InvalidOperationException("Failed to generate release summaries.");

ReleaseHistory history = Summary.GetReleaseCalendar(summaries);
Summary.PopulateCveInformation(history, inputDir);

// Generate ship history index files (archives/index.json, year/month indexes)
await ShipIndexFiles.GenerateAsync(inputDir, outputDir, history);

// Display skipped files count
Console.WriteLine($"Skipped {ShipIndexFiles.SkippedFilesCount} files because they did not change.");

return 0;
