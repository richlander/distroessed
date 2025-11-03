using DotnetRelease;
using VersionIndex;

// Generates .NET version index files (major version -> patch version hierarchy)
// - Root index.json with all major versions
// - Per-major-version index.json files (e.g., 8.0/index.json)
// - SDK index files for .NET 8.0+
// - Manifest files with lifecycle information

if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: VersionIndex <input-directory> [output-directory]");
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
