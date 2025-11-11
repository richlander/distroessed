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
```

## Usage

Both VersionIndex and ShipIndex tools use this library to:
1. Load CVE data from `cve.json` files
2. Convert full CVE disclosures to summary format
3. Filter CVE records by release version
4. Ensure consistent CVE data formatting

## Dependencies

- **DotnetRelease**: For CVE data structures (`CveRecords`, `CveRecordSummary`, etc.)
