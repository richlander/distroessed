using DotnetRelease;
using DotnetRelease.Summary;
using ShipIndex;

// Generates .NET ship history index files (chronological: years -> months -> days)
// - Root archives/index.json with all years
// - Per-year archives/{year}/index.json files
// - Per-month archives/{year}/{month}/index.json files
// - CVE information linked to ship days

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: ShipIndex <input-directory> [output-directory] [--commit <sha>]");
    Console.Error.WriteLine("  input-directory:  Directory containing release-notes data to read");
    Console.Error.WriteLine("  output-directory: Directory to write generated index files (optional, defaults to input-directory)");
    Console.Error.WriteLine("  --commit <sha>:   Git commit SHA to use in generated links (optional, defaults to 'main')");
    return 1;
}

string? inputDir = null;
string? outputDir = null;
string? commitSha = null;

// Parse arguments
for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--commit" && i + 1 < args.Length)
    {
        commitSha = args[++i];
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

// Set commit SHA if provided
if (commitSha != null)
{
    Location.SetGitHubCommit(commitSha);
    Console.WriteLine($"Using commit: {commitSha}");
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
