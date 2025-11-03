# ShipIndex Tool

Generates .NET ship history index files organized chronologically by when releases were shipped.

## Purpose

Creates a time-based release calendar:
- Root `archives/index.json` - Lists all years with releases
- Per-year `archives/{year}/index.json` - Lists months with releases
- Per-month `archives/{year}/{month}/index.json` - Lists releases on specific days
- CVE information linked to ship days

## Usage

```bash
dotnet run --project ShipIndex -- <input-directory> [output-directory]
```

**Arguments:**
- `input-directory` - Directory containing release-notes data (e.g., `~/git/core-rich/release-notes`)
- `output-directory` - Optional. Where to write generated files (defaults to input-directory)

**Example:**
```bash
dotnet run --project ShipIndex -- ~/git/core-rich/release-notes
```

## Generated Files

- `archives/index.json` - Root ship history index with all years
- `archives/{year}/index.json` - Year index with all months (e.g., `archives/2024/index.json`)
- `archives/{year}/{month}/index.json` - Month index with ship days (e.g., `archives/2024/11/index.json`)
- CVE records linked to relevant ship days

## Cross-Linking

Generates links to VersionIndex based on shipped versions:
- Ship day entries link to `{version}/{patch}/release.json` files
- Month/year aggregations reference major version indexes
- Assumes VersionIndex source files exist (convention over validation)

## Design

- **Single-pass execution** - Reads from source release.json files
- **Cache-friendly** - Past months/years are immutable
- **Independent** - Can run in any order relative to VersionIndex
- **CVE-authoritative** - Owns the canonical CVE records

See `/docs/version-ship-cross-linking.md` for cross-linking design details.

## Use Cases

- "What shipped in November 2024?"
- "Show me all releases in 2024"
- "What CVEs were fixed on a specific date?"
- "Which versions were part of the December ship day?"
