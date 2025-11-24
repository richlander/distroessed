# CveSynthesize Implementation

## Overview
This tool generates historical `cve.json` files for the timeline structure from data in `releases.json` files. It synthesizes CVE data for releases before 2023-11 when timeline CVE files were first created.

## Features Implemented

### 1. CVE Discovery
- Scans all major version directories for `releases.json` files
- Extracts CVE data from the `cve-list` property of each release
- Groups releases by year-month based on their release dates
- Filters out releases that occur after the earliest existing timeline CVE file (cutoff date)

### 2. MSRC Data Integration
The tool fetches additional CVE metadata from the Microsoft Security Response Center (MSRC) API, similar to CveValidate:

- **CVSS Scores and Vectors**: Base scores and vector strings from MSRC
- **CWE/Weakness Information**: Common Weakness Enumeration IDs
- **CNA Data**:
  - Severity ratings (Low, Moderate, Important, Critical)
  - Impact categories (Denial of Service, Remote Code Execution, etc.)
  - Acknowledgments for security researchers
  - FAQ entries with questions and answers

### 3. CVE JSON File Generation
For each month with CVE data:
- Creates directory structure: `timeline/{year}/{month}/`
- Builds complete `CveRecords` structure with:
  - **Disclosures**: Full CVE records with MSRC data
  - **Products**: Affected product entries
  - **Packages**: Affected package entries (empty for now)
  - **Dictionaries**: Generated lookup dictionaries for efficient querying
    - `cve_releases`: CVE IDs by release version
    - `release_cves`: Release versions by CVE ID
    - `product_cves`: CVE IDs by product name
    - `package_cves`: CVE IDs by package name
    - `product_name`: Display names for products

### 4. Shared Logic with CveValidate
The implementation reuses key functions from CveValidate:
- `FetchMsrcData()`: Fetches MSRC XML from the API
- `ParseMsrcXml()`: Parses embedded HTML tables and XML to extract CVE metadata
- `GenerateDictionaries()`: Creates lookup dictionaries from CVE data
- `GetProductDisplayName()`: Maps product names to display names

## Usage

```bash
# Synthesize historical CVE files
CveSynthesize ~/git/core/release-notes

# The tool will:
# 1. Find the earliest existing timeline CVE file (e.g., 2023-11)
# 2. Process all releases before that date
# 3. Group CVEs by month
# 4. Fetch MSRC data for each month
# 5. Generate cve.json files in timeline/{year}/{month}/
```

## Architecture

```
releases.json files          MSRC API
       ↓                        ↓
    [Extract CVEs]        [Fetch metadata]
       ↓                        ↓
    [Group by month] ← [Enrich CVE data]
       ↓
    [Generate dictionaries]
       ↓
    timeline/{year}/{month}/cve.json
```

## Data Flow

1. **Scan Phase**: Read all `releases.json` files to find CVEs
2. **Grouping Phase**: Group releases by year-month
3. **Enrichment Phase**: Fetch MSRC data for each month
4. **Generation Phase**: Build complete CVE records with all metadata
5. **Output Phase**: Write `cve.json` files to timeline directories

## Shared Code Location

As documented in `src/CveHandler/README.md`, the shared CVE handling code should be moved to the CveHandler library for better code reuse between tools like CveSynthesize, CveValidate, VersionIndex, and ShipIndex.

Currently duplicated functions that should be moved to CveHandler:
- MSRC data fetching and parsing
- Dictionary generation
- Product name mapping

## Future Enhancements

1. **Move Shared Code**: Extract MSRC fetching/parsing to CveHandler library
2. **Package Support**: Add package-level CVE tracking
3. **Commit Information**: Include commit hashes that fix vulnerabilities
4. **Better Descriptions**: Enhance CVE descriptions beyond generic text
5. **Error Recovery**: Add retry logic for MSRC API failures
