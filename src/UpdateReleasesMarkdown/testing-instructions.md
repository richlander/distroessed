# Testing UpdateReleasesMarkdown

You can test this tool with `dotnet run`. It requires a clone of richlander/core to run. It is important that any content or tools you download are not added to git history.

## .gitignore

The `.gitignore` for the repo has these lines:

```gitignore
# .NET installation scripts and tools (DO NOT COMMIT) - from dotnet-install-for-agents
_temp/
# .NET SDK installation directory (if installed locally)
.dotnet/
```

That means that these directories are safe to use and will not be added to git history.

## Instructions

You need to clone the richlander/core repo. Clone it to a location that will not add to git history. The example uses the `_temp` directory.

Clone command: `git clone https://github.com/richlander/core`

## Single Directory Mode (Default Behavior)

The tool can be called with a single directory location, like the following. In this mode, the output file is created in the current directory:

```bash
dotnet run --project src/UpdateReleasesMarkdown _temp/core/release-notes
```

This will create `releases.md` in the current directory.

## Two Directory Mode (Specified Output)

The tool can be called with separate input and output paths. This enables targeted testing with read-only input and allows multiple tests to run concurrently:

```bash
dotnet run --project src/UpdateReleasesMarkdown _temp/core/release-notes _temp/test-output/releases.md
```

In this mode:
- Input directory: `_temp/core/release-notes` (read-only access)
- Output file: `_temp/test-output/releases.md` (will be created if the directory doesn't exist)

This approach allows you to:
- Keep the original release-notes directory unchanged
- Run multiple concurrent tests with different output directories
- Test the tool's behavior without affecting the source data

## Template File

The tool uses a template file located at `templates/releases-template.md`. This template contains the structure for the releases.md file with placeholders for:

- `{{SUPPORTED_RELEASES}}` - Table of supported releases
- `{{EOL_RELEASES}}` - Table of end-of-life releases  
- `{{RELEASE_LINKS}}` - Markdown links to specific patch versions

## Validation

To validate the output, check the generated `releases.md` file:

1. **Structure**: Should have supported releases table and end-of-life releases table
2. **Data**: Should show correct release dates, types, and latest patch versions
3. **Links**: Should have proper markdown links to release notes and patch versions
4. **Formatting**: Should match the format of the original releases.md in richlander/core

## Expected Output

The tool should generate a releases.md file that includes:

- Supported releases (currently .NET 9.0 and 8.0) with latest patch versions
- End-of-life releases (currently .NET 7.0, 6.0, 5.0, etc.) with final patch versions
- Proper markdown links to release notes and specific patch versions
- Correct date formatting and release type information

## Troubleshooting

If you encounter issues:

1. **Template not found**: Ensure `templates/releases-template.md` exists
2. **Index.json not found**: Verify the core repo path points to a valid release-notes directory
3. **Missing patch versions**: Some older versions may not have complete index.json files
4. **Build errors**: Ensure .NET 10.0 SDK is installed and all project references are available

## Example Test Run

```bash
# Clone the core repo
git clone https://github.com/richlander/core _temp/core

# Create output directory
mkdir -p _temp/test-output

# Run the tool
dotnet run --project src/UpdateReleasesMarkdown _temp/core/release-notes _temp/test-output/releases.md

# Check the output
cat _temp/test-output/releases.md
```