# ShipIndex Tool

Generates .NET ship history index files organized chronologically by when releases were shipped.

## Purpose

Creates a time-based release calendar:
- Root `release-history/index.json` - Lists all years with releases
- Per-year `release-history/{year}/index.json` - Lists months with releases
- Per-month `release-history/{year}/{month}/index.json` - Lists releases on specific days
- CVE information linked to ship days

## Usage

```bash
dotnet run --project ShipIndex -- <input-directory> [output-directory] [--url-root <url>]
```

**Arguments:**
- `input-directory` - Directory containing release-notes data (e.g., `~/git/core/release-notes`)
- `output-directory` - Optional. Where to write generated files (defaults to input-directory)
- `--url-root <url>` - Optional. Base URL root (before `/release-notes/`) for generated links (defaults to GitHub main)

**Examples:**
```bash
# Generate with default GitHub main branch links
dotnet run --project ShipIndex -- ~/git/core/release-notes

# Generate with specific commit links (cache-busting for testing)
dotnet run --project ShipIndex -- ~/git/core/release-notes --url-root https://raw.githubusercontent.com/dotnet/core/abc123def456

# Generate with custom CDN or mirror
dotnet run --project ShipIndex -- ~/git/core/release-notes --url-root https://my-cdn.example.com/dotnet/core
```

**Note:** The `--url-root` should not include `/release-notes/` - it will be appended automatically.

## Generated Files

- `release-history/index.json` - Root ship history index with all years
- `release-history/{year}/index.json` - Year index with all months (e.g., `release-history/2024/index.json`)
- `release-history/{year}/{month}/index.json` - Month index with ship days (e.g., `release-history/2024/11/index.json`)
- CVE records linked to relevant ship days

### Navigation Links

Each year and month index includes HAL+JSON `next` and `prev` link relations for chronological navigation:
- **Year indexes** include `next` and `prev` links to adjacent years (when they exist)
- **Month indexes** include `next` and `prev` links to adjacent months within the same year (when they exist)

These links enable sequential navigation through the timeline without requiring knowledge of the full structure.

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

> **Note on CVE Files**: The `last-updated` field in `cve.json` files should be set to the current date when initially publishing the history index (baseline date). The disclosure dates in individual CVE records represent when vulnerabilities were publicly disclosed, but the `last-updated` field tracks when the CVE data file itself was last modified.

See `/docs/version-ship-cross-linking.md` for cross-linking design details.

## Use Cases

- "What shipped in November 2024?"
- "Show me all releases in 2024"
- "What CVEs were fixed on a specific date?"
- "Which versions were part of the December ship day?"
