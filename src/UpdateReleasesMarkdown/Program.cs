using DotnetRelease;
using MarkdownHelpers;
using System.Text.Json;
using UpdateReleasesMarkdown;

// Usage:
// UpdateReleasesMarkdown <core-repo-path> [output-path]
// UpdateReleasesMarkdown ~/git/core/release-notes ~/output/releases.md

Console.WriteLine("UpdateReleasesMarkdown");

if (args.Length is < 1)
{
    ReportInvalidArgs();
    return;
}

string coreRepoPath = args[0];
string outputPath = args.Length > 1 ? args[1] : "releases.md";
string templatePath = "templates/releases-template.md";

if (!Directory.Exists(coreRepoPath))
{
    Console.WriteLine($"Core repo path '{coreRepoPath}' does not exist.");
    ReportInvalidArgs();
    return;
}

if (!File.Exists(templatePath))
{
    Console.WriteLine($"Template file '{templatePath}' not found.");
    ReportInvalidArgs();
    return;
}

try
{
    await ReleasesReport.MakeReport(coreRepoPath, outputPath, templatePath);
    Console.WriteLine($"Successfully generated releases.md at: {outputPath}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error generating report: {ex.Message}");
    return;
}

static void ReportInvalidArgs()
{
    Console.WriteLine("Invalid args.");
    Console.WriteLine("Usage: UpdateReleasesMarkdown <core-repo-path> [output-path]");
    Console.WriteLine("  core-repo-path: Path to the richlander/core repository");
    Console.WriteLine("  output-path:   Output path for releases.md (default: releases.md)");
}
