# UpdateReleasesMarkdown

A .NET tool that generates a `releases.md` file by reading HAL+JSON index files from the richlander/core repository.

## Purpose

This tool reads the HAL+JSON index.json files in the richlander/core repository and generates a comprehensive releases.md file that lists all .NET releases, separating supported releases from end-of-life releases.

## Usage

```bash
dotnet run --project src/UpdateReleasesMarkdown <core-repo-path> [output-path]
```

### Parameters

- `core-repo-path`: Path to the richlander/core repository's release-notes directory
- `output-path`: (Optional) Output path for the generated releases.md file. Defaults to `releases.md` in the current directory.

### Examples

```bash
# Generate releases.md in current directory
dotnet run --project src/UpdateReleasesMarkdown _temp/core/release-notes

# Generate releases.md in specified location
dotnet run --project src/UpdateReleasesMarkdown _temp/core/release-notes _temp/output/releases.md
```

## Features

- Reads main index.json file to get all .NET versions
- Reads version-specific index.json files to get latest patch versions
- Separates supported vs end-of-life releases based on lifecycle data
- Generates properly formatted markdown tables
- Creates markdown links to release notes and specific patch versions
- Uses templating system for consistent output format

## Data Sources

The tool reads from:
- `index.json` - Main index with all .NET versions and lifecycle information
- `{version}/index.json` - Version-specific index with patch release information

## Output Format

The generated releases.md file includes:
- Supported releases table with version, release date, type, phase, latest patch, and EOL date
- End-of-life releases table with version, release date, support type, final patch, and EOL date
- Markdown links to release notes and specific patch versions

## Testing

See [testing-instructions.md](testing-instructions.md) for detailed testing instructions.