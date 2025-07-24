using DotnetRelease;

namespace UpdateIndexes;

/// <summary>
/// Simple test to verify the template-based llms.txt generation works correctly.
/// This is included in the UpdateIndexes project for testing purposes.
/// </summary>
public static class LlmsTxtGeneratorTest
{
    public static void RunTest()
    {
        Console.WriteLine("Running LlmsTxtGenerator template test...");
        
        // Create sample HAL+JSON links data
        var sampleLinks = new Dictionary<string, HalLink>
        {
            { "self", new HalLink("https://api.nuget.org/v3-flatcontainer/release-notes/index.json")
                {
                    Title = ".NET Release Index",
                    Type = "application/hal+json"
                }
            },
            { "help-markdown-raw", new HalLink("https://raw.githubusercontent.com/dotnet/core/main/release-notes/usage.md")
                {
                    Title = "Usage Guide (Raw Markdown)",
                    Type = "application/markdown"
                }
            },
            { "glossary-markdown-raw", new HalLink("https://raw.githubusercontent.com/dotnet/core/main/release-notes/glossary.md")
                {
                    Title = "Glossary (Raw Markdown)",
                    Type = "application/markdown"
                }
            },
            { "newest-release", new HalLink("https://api.nuget.org/v3-flatcontainer/release-notes/9.0/index.json")
                {
                    Title = "Latest .NET release (.NET 9.0)",
                    Type = "application/hal+json"
                }
            },
            { "lts-release", new HalLink("https://api.nuget.org/v3-flatcontainer/release-notes/8.0/index.json")
                {
                    Title = "LTS release (.NET 8.0)",
                    Type = "application/hal+json"
                }
            },
            { "stable-sdk-downloads", new HalLink("https://raw.githubusercontent.com/richlander/core/main/{version}/sdk/sdk.json")
                {
                    Title = "Stable SDK download links (template: replace {version} with version number)",
                    Type = "application/hal+json",
                    Templated = true
                }
            },
            { "archives", new HalLink("https://api.nuget.org/v3-flatcontainer/release-notes/archives/index.json")
                {
                    Title = "Security Advisories",
                    Type = "application/hal+json"
                }
            }
        };
        
        // Generate llms.txt using the template system
        var result = LlmsTxtGenerator.Generate(sampleLinks, "Test Release Metadata", "Test description for AI assistants.");
        
        Console.WriteLine("Generated llms.txt content:");
        Console.WriteLine("=" + new string('=', 50));
        Console.WriteLine(result);
        Console.WriteLine("=" + new string('=', 50));
        
        // Basic validation
        if (result.Contains("Test Release Metadata") && 
            result.Contains("Test description for AI assistants") &&
            result.Contains("Getting Started") &&
            result.Contains("Key Data Sources") &&
            result.Contains("Release index") &&
            result.Contains("Latest release"))
        {
            Console.WriteLine("✓ Template test PASSED - all expected content found!");
        }
        else
        {
            Console.WriteLine("✗ Template test FAILED - missing expected content!");
        }
        
        Console.WriteLine();
    }
}