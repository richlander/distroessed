# CveSynthesize

Tool for synthesizing historical CVE JSON files from releases.json data.

## Purpose

Timeline CVE files (`timeline/{year}/{month}/cve.json`) were first created in 2023-11. This tool creates CVE files for earlier releases by extracting CVE data from `releases.json` files.

This enables:
- Patch-level index.json files for pre-8.0 versions (6.0, 7.0, etc.)
- Complete timeline CVE view back to .NET Core 1.0
- Consistent CVE data structure across all .NET versions

## Usage

```bash
CveSynthesize <release-notes-path>
```

Example:
```bash
CveSynthesize ~/git/core/release-notes
```

## Process

1. Scans all `releases.json` files for CVE data
2. Groups CVEs by release month (year-month)
3. Stops at the earliest existing timeline cve.json file (2023-11)
4. Generates properly structured `cve.json` files for historical months

## Implementation Status

**Phase 1** (Current): Scanning and discovery
- ✅ Finds earliest timeline CVE file
- ✅ Scans releases.json for CVE data
- ✅ Groups by year-month
- ⏳ Generate cve.json files (next phase)

**Phase 2** (Planned): CVE data enrichment
- Fetch full CVE details from CVE.org API
- Extract CVSS scores, severity, descriptions
- Build proper data structures (disclosures, products, packages)

**Phase 3** (Planned): Integration
- Update VersionIndex to check for timeline CVE files
- Generate patch indexes for older versions
- Validation and testing

## Dependencies

- **DotnetRelease**: CVE data structures
- **CveHandler**: Shared CVE loading and transformation logic
