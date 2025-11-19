# ReleasesMd

A command-line tool to generate the `releases.md` file from .NET release-notes data.

## Purpose

This tool automatically generates the `releases.md` file (which lists all .NET releases, both supported and end-of-life) by reading data from the release-notes index files. This replaces the need to manually edit the releases.md file.

## Usage

```bash
ReleasesMd generate <output-path> [source-path]
```

### Arguments

- `output-path` - Path where releases.md will be written
- `source-path` - Optional path or URL to release-notes directory (default: GitHub release-index)

### Examples

Generate releases.md using local release-notes:
```bash
ReleasesMd generate ~/git/core/releases.md ~/git/core/release-notes
```

Generate to a specific path:
```bash
ReleasesMd generate releases.md ~/git/core/release-notes
```

## Data Sources

The tool reads the following data:
- `release-notes/releases-index.json` - Main index with all release versions
- `release-notes/{version}/manifest.json` - Individual version manifests for:
  - Initial release dates
  - Release announcement blog URLs
  - End-of-life blog URLs

## Output

The generated `releases.md` file includes:
- Header section with overview text
- Supported releases table with:
  - Version
  - Release Date (with link to announcement blog)
  - Release type (LTS/STS)
  - Support phase
  - Latest patch version (with link to release notes)
  - End of support date
- End-of-life releases table with similar information
- Reference links at the bottom

## Modeled On

This tool is modeled on the `SupportedOsMd` application, following similar patterns for:
- Command-line argument handling
- Path adaptor usage (supporting both local paths and URLs)
- Markdown generation using the MarkdownHelpers library
- Table and link generation
