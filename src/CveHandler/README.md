# CveHandler

Shared library for loading and transforming CVE (Common Vulnerabilities and Exposures) data.

## Purpose

This library provides common CVE handling utilities used by both VersionIndex and ShipIndex tools to ensure consistent CVE data processing across the .NET release documentation system.

## Components

### CveLoader
Loads CVE data from JSON files.

```csharp
// Load CVE records from a specific file
var cveRecords = await CveLoader.LoadCveRecordsAsync("path/to/cve.json");

// Load CVE records from a directory (looks for cve.json)
var cveRecords = await CveLoader.LoadCveRecordsFromDirectoryAsync("path/to/directory");

// Load CVE records from timeline directory for a specific release date
var cveRecords = await CveLoader.LoadCveRecordsForReleaseDateAsync(
    releaseNotesRoot: "/path/to/release-notes",
    releaseDate: new DateTimeOffset(2025, 3, 11, 0, 0, 0, TimeSpan.Zero)
);
// This loads from /path/to/release-notes/timeline/2025/03/cve.json
```

### CveTransformer
Transforms CVE data between different formats.

```csharp
// Convert full CVE disclosures to summary format for embedding in indexes
var summaries = CveTransformer.ToSummaries(cveRecords);

// Extract just CVE IDs
var cveIds = CveTransformer.ExtractCveIds(cveRecords);

// Filter CVE records for a specific release version
var filtered = CveTransformer.FilterByRelease(cveRecords, "9.0.0");

// Validate CVE data consistency between releases.json and cve.json
CveTransformer.ValidateCveData(
    releaseVersion: "9.0.3",
    cveIdsFromRelease: cveIdsFromReleasesJson,
    cveIdsFromCveJson: cveIdsFromTimelineCveJson
);
// Logs warnings if there are mismatches
```

## Usage

Both VersionIndex and ShipIndex tools use this library to:
1. Load CVE data from `cve.json` files in the timeline directories (single source of truth)
2. Convert full CVE disclosures to summary format
3. Filter CVE records by release version
4. Validate CVE data consistency between `releases.json` and timeline `cve.json`
5. Ensure consistent CVE data formatting

## Architecture

CVE JSON files exist **only** in timeline directories (e.g., `/timeline/2025/03/cve.json`). Both VersionIndex and ShipIndex load from these locations:
- **ShipIndex**: Generates timeline indexes, loads CVE data from the month directory
- **VersionIndex**: Generates version indexes, loads CVE data from timeline based on release date

This avoids duplication and ensures a single source of truth for CVE data.

## Dependencies

- **DotnetRelease**: For CVE data structures (`CveRecords`, `CveRecordSummary`, etc.)
