# CveSynthesize

Tool for synthesizing historical CVE JSON files from releases.json data.

## Purpose

Timeline CVE files (`timeline/{year}/{month}/cve.json`) were first created in 2023-11. This tool creates CVE files for earlier releases by extracting CVE data from `releases.json` files and enriching them with MSRC metadata.

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
4. Fetches MSRC data (CVSS scores, severity, CWE, acknowledgments, FAQs)
5. Generates properly structured `cve.json` files for historical months

## Implementation Status

**Phase 1** (Complete): Scanning and discovery
- ✅ Finds earliest timeline CVE file
- ✅ Scans releases.json for CVE data
- ✅ Groups by year-month

**Phase 2** (Complete): CVE data enrichment
- ✅ Fetch MSRC data from api.msrc.microsoft.com
- ✅ Extract CVSS scores, vectors, severity ratings
- ✅ Extract CWE/weakness information
- ✅ Extract CNA data (severity, impact, acknowledgments, FAQs)
- ✅ Build proper data structures (disclosures, products, packages)
- ✅ Generate lookup dictionaries (cve_releases, product_cves, etc.)

**Phase 3** (Complete): File generation
- ✅ Create timeline directory structure
- ✅ Write cve.json files with full metadata
- ✅ Skip existing files (idempotent operation)

**Phase 4** (Future): Integration
- ⏳ Update VersionIndex to use synthesized CVE files
- ⏳ Generate patch indexes for older versions
- ⏳ Validation and testing

## Dependencies

- **DotnetRelease**: CVE data structures
- **CveHandler**: Shared CVE loading and transformation logic

## MSRC Data Integration

Like CveValidate, this tool fetches additional CVE metadata from Microsoft Security Response Center (MSRC):

- **CVSS v3.1 Scores**: Base score and vector string
- **Severity**: CNA severity rating (Low, Moderate, Important, Critical)
- **Impact**: Vulnerability impact type (Denial of Service, RCE, etc.)
- **CWE**: Common Weakness Enumeration identifier
- **Acknowledgments**: Security researchers who reported the vulnerability
- **FAQs**: Questions and answers from Microsoft about the CVE

## Notes

- The tool is idempotent - it skips months that already have cve.json files
- MSRC data fetching may fail for very old releases (pre-2016) - the tool continues with partial data
- Generated files use the same structure as manually created timeline CVE files
- Dictionary generation logic matches CveValidate to ensure consistency
