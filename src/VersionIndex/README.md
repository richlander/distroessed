# VersionIndex Tool

Generates .NET version-centric index files for the release notes repository.

## Purpose

Creates a hierarchical version index:
- Root `index.json` - Lists all major .NET versions
- Per-major-version `{version}/index.json` - Lists all patch releases
- SDK indexes for .NET 8.0+ - SDK feature bands and download links
- Manifest files with lifecycle information

## Usage

```bash
dotnet run --project VersionIndex -- <input-directory> [output-directory]
```

**Arguments:**
- `input-directory` - Directory containing release-notes data (e.g., `~/git/core-rich/release-notes`)
- `output-directory` - Optional. Where to write generated files (defaults to input-directory)

**Example:**
```bash
dotnet run --project VersionIndex -- ~/git/core-rich/release-notes
```

## Generated Files

- `index.json` - Root version index with all major versions
- `{version}/index.json` - Patch version index (e.g., `8.0/index.json`)
- `{version}/manifest.json` - Lifecycle and support information (includes sdk-index link)
- `{version}/sdk/index.json` - SDK index with latest downloads + feature bands (for .NET 8.0+)
- `{version}/sdk/sdk-{version}.{band}xx.json` - Feature band specific download links

## Cross-Linking

Generates links to ShipIndex (archives) based on release dates:
- Patch versions link to `archives/{year}/{month}/index.json` for their ship day
- Assumes ShipIndex will exist (convention over validation)

## Design

- **Single-pass execution** - Reads from source files only
- **Cache-friendly** - Root index rarely changes
- **Independent** - Can run in any order relative to ShipIndex

See `/docs/version-ship-cross-linking.md` for cross-linking design details.
