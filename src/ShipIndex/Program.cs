using DotnetRelease;
using DotnetRelease.Summary;
using ShipIndex;

// Generates .NET ship timeline index files (chronological: years -> months -> days)
// - Root timeline/index.json with all years
// - Per-year timeline/{year}/index.json files
// - Per-month timeline/{year}/{month}/index.json files
// - CVE information linked to ship days

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ShipIndex <input-directory> [output-directory] [--url-root <url>]");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes data to read");
    Console.Error.WriteLine("  output-directory: Directory to write generated index files (optional, defaults to input-directory)");
    Console.Error.WriteLine("  --url-root <url>: Base URL root (before /release-notes/) for generated links (optional, defaults to GitHub main)");
    Console.Error.WriteLine("                    Example: https://raw.githubusercontent.com/dotnet/core/commit-sha");
    return 1;
}

string? inputDir = null;
string? outputDir = null;
string? urlRoot = null;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--url-root" && i + 1 < args.Length)
    {
        urlRoot = args[++i];
    }
    else if (inputDir == null)
    {
        inputDir = args[i];
    }
    else if (outputDir == null)
    {
        outputDir = args[i];
    }
}

if (inputDir == null)
{
    Console.Error.WriteLine("Error: input-directory is required");
    return 1;
}

outputDir ??= inputDir;

if (!Directory.Exists(inputDir))
{
    Console.Error.WriteLine($"Input directory not found: {inputDir}");
    return 1;
}

// Set URL root if provided
if (urlRoot != null)
{
    Location.SetUrlRoot(urlRoot);
    Console.WriteLine($"Using URL root: {urlRoot}");
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

// Generate ship timeline index files (timeline/index.json, year/month indexes)
await ShipIndexFiles.GenerateAsync(inputDir, outputDir, history);

// Display skipped files count
Console.WriteLine($"Skipped {ShipIndexFiles.SkippedFilesCount} files because they did not change.");

return 0;
