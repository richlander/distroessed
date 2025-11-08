using DotnetRelease;
using VersionIndex;

// Generates .NET version index files (major version -> patch version hierarchy)
// - Root index.json with all major versions
// - Per-major-version index.json files (e.g., 8.0/index.json)
// - SDK index files for .NET 8.0+
// - Manifest files with lifecycle information

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: VersionIndex <input-directory> [output-directory] [--commit <sha>]");
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
ReleaseIndexFiles.ResetSkippedFilesCount();

// Generate release summaries from source data
var summaries = await Summary.GetReleaseSummariesAsync(inputDir) 
    ?? throw new InvalidOperationException("Failed to generate release summaries.");

// Generate version index files (main index, per-major-version indexes, manifests)
await ReleaseIndexFiles.GenerateAsync(summaries, inputDir, outputDir);

// Generate SDK index files for supported versions
await SdkIndexFiles.GenerateAsync(summaries, outputDir);

// Display skipped files count
Console.WriteLine($"Skipped {ReleaseIndexFiles.SkippedFilesCount} files because they did not change.");

return 0;
