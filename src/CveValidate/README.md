# CveValidate

A tool to validate and update `cve.json` files.

## Purpose

The `cve.json` files contain CVE (Common Vulnerabilities and Exposures) information along with query dictionaries. This tool:
- Validates taxonomy (products, platforms, architectures, severity, CNA)
- Validates version coherence (min ≤ max < fixed)
- Validates foreign key relationships
- Validates query dictionaries (product_name, product_cves, cve_releases, release_cves, cve_commits)
- Validates URLs and MSRC data (enabled by default, can be skipped with --skip-urls)
- Updates query dictionaries, cve_commits, and MSRC data

## Usage

### Validate Files

Validate one or more `cve.json` files:

```bash
# Validate a single file
CveValidate validate ~/git/core/release-notes/archives/2024/01/cve.json

# Validate all cve.json files in a directory tree
CveValidate validate ~/git/core/release-notes/archives

# Validate without checking URLs and MSRC (faster, offline-friendly)
CveValidate ~/git/core/release-notes/archives --skip-urls
```

The tool will report any validation errors found:
- Invalid taxonomy values
- Incoherent version numbers
- Foreign key violations
- Dictionary discrepancies
- Broken URLs (when not using --skip-urls)
- MSRC data mismatches (when not using --skip-urls)

### Update Dictionaries

Generate and update query dictionaries and cve_commits in one or more `cve.json` files:

```bash
# Update a single file
CveValidate update ~/git/core/release-notes/archives/2024/01/cve.json

# Update all cve.json files in a directory tree
CveValidate update ~/git/core/release-notes/archives
```

The tool will:
1. Read the existing `cve.json` file
2. Generate the dictionaries from the `products` and `packages` sections
3. Generate the `cve_commits` dictionary from product and package commits
4. Fetch CVSS scores from CVE.org API
5. Fetch CNA data (severity, impact, acknowledgments, FAQs) from MSRC (unless --skip-urls is used)
6. Update the file with the corrected dictionaries and data

## Dictionary Types

### Query Dictionaries

#### 1. `product_name`
Maps product identifiers to human-readable display names.

Example:
```json
"product_name": {
  "dotnet-runtime-libraries": ".NET Runtime Libraries",
  "dotnet-runtime-aspnetcore": "ASP.NET Core Runtime"
}
```

#### 2. `product_cves`
Maps product names to lists of CVE IDs that affect them.

Example:
```json
"product_cves": {
  "dotnet-runtime-libraries": ["CVE-2024-0057"],
  "Microsoft.Data.SqlClient": ["CVE-2024-0056"]
}
```

#### 3. `cve_releases`
Maps CVE IDs to lists of release versions affected by them.

Example:
```json
"cve_releases": {
  "CVE-2024-0057": ["6.0", "7.0", "8.0"]
}
```

#### 4. `release_cves`
Maps release versions to lists of CVE IDs affecting them.

Example:
```json
"release_cves": {
  "6.0": ["CVE-2024-0057", "CVE-2024-21319"],
  "7.0": ["CVE-2024-0057", "CVE-2024-21319"]
}
```

#### 5. `cve_commits`
Maps CVE IDs to lists of commit hashes that fix them.

Example:
```json
"cve_commits": {
  "CVE-2024-0057": ["abc123", "def456"],
  "CVE-2024-0056": ["ghi789"]
}
```

## Validations

### Taxonomy Validation
- Products must be: `dotnet-runtime`, `dotnet-aspnetcore`, `dotnet-windows-desktop`, `dotnet-sdk`
- Platforms must be: `linux`, `macos`, `windows`, `all`
- Architectures must be: `arm`, `arm64`, `x64`, `x86`, `all`
- Severity must be: `critical`, `high`, `medium`, `low`
- CNA must be: `microsoft`

### Release Version Format Validation
- **Products**: The `release` field must be a two-part version (e.g., `9.0`, `8.0`)
- **Packages**: The `release` field can be multi-part versions (e.g., `9.0.1.2`, `8.0.5`)

### Version Coherence
Validates that for each product/package: `minVulnerable ≤ maxVulnerable < fixed`

Handles semantic versioning including prerelease tags (e.g., `1.0.0-preview.1 < 1.0.0`).

### Foreign Key Validation
- Products and packages must reference existing CVEs
- cve_commits must reference existing CVEs and commits
- All CVEs must be referenced by at least one product or package
- All commits must be referenced by at least one CVE

### Dictionary Validation
- All query dictionaries are validated against generated values
- Missing or extra keys are reported
- Value mismatches are reported with details

### Commits Consistency
When a `commits` dictionary exists:
- All products must have non-null, non-empty commits arrays
- All packages must have non-null, non-empty commits arrays

### URL and MSRC Validation
When not skipped (enabled by default), validates that:
- All URLs in CVE references return HTTP 200-299
- All commit URLs are accessible
- MSRC data matches (CVSS scores, vectors, CWE, CNA severity, CNA impact)

Use `--skip-urls` to skip these validations for faster, offline operation.

## How It Works

### Dictionary Generation
The tool generates dictionaries by:
1. Iterating through all entries in the `products` array
2. Iterating through all entries in the `packages` array
3. Building the dictionaries based on product/package names, CVE IDs, and release versions
4. Sorting all lists for consistency

**Note:** Products or packages with empty `release` fields are not included in the `cve_releases` and `release_cves` dictionaries.

### cve_commits Generation
The tool generates `cve_commits` by:
1. Collecting all commit hashes from product.Commits arrays
2. Collecting all commit hashes from package.Commits arrays
3. Grouping by CVE ID and removing duplicates
4. Sorting the result

## Building

```bash
dotnet build src/CveValidate/CveValidate.csproj
```

## Output Format

The tool generates JSON files with 2-space indentation, matching the standard format used by the .NET CVE JSON files.

## Running

```bash
dotnet run --project src/CveValidate/CveValidate.csproj -- <command> <path> [options]
```

Or build and run the published version:

```bash
dotnet publish src/CveValidate/CveValidate.csproj -c Release
./artifacts/bin/CveValidate/release/CveValidate <command> <path> [options]
```
