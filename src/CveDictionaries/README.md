# CveDictionaries

A tool to validate and generate query dictionaries for `cve.json` files.

## Purpose

The `cve.json` files contain CVE (Common Vulnerabilities and Exposures) information along with query dictionaries at the end of the file. These dictionaries enable efficient querying of:
- Which products are affected by which CVEs
- Which releases are affected by which CVEs
- Product display names

This tool ensures these dictionaries are accurate and can regenerate them when needed.

## Usage

### Validate Dictionaries

Validate that the query dictionaries in one or more `cve.json` files are correct:

```bash
# Validate a single file
CveDictionaries validate ~/git/core/release-notes/archives/2024/01/cve.json

# Validate all cve.json files in a directory tree
CveDictionaries validate ~/git/core/release-notes/archives
```

The tool will report any discrepancies found in the dictionaries:
- Missing or extra keys
- Incorrect values

### Generate Dictionaries

Generate and update the query dictionaries in one or more `cve.json` files:

```bash
# Generate for a single file
CveDictionaries generate ~/git/core/release-notes/archives/2024/01/cve.json

# Generate for all cve.json files in a directory tree
CveDictionaries generate ~/git/core/release-notes/archives
```

The tool will:
1. Read the existing `cve.json` file
2. Generate the dictionaries from the `products` and `packages` sections
3. Update the file with the corrected dictionaries

## Dictionary Types

The tool validates and generates four types of dictionaries:

### 1. `product_name`
Maps product identifiers to human-readable display names.

Example:
```json
"product_name": {
  "dotnet-runtime-libraries": ".NET Runtime Libraries",
  "dotnet-runtime-aspnetcore": "ASP.NET Core Runtime"
}
```

### 2. `product_cves`
Maps product names to lists of CVE IDs that affect them.

Example:
```json
"product_cves": {
  "dotnet-runtime-libraries": ["CVE-2024-0057"],
  "Microsoft.Data.SqlClient": ["CVE-2024-0056"]
}
```

### 3. `cve_releases`
Maps CVE IDs to lists of release versions affected by them.

Example:
```json
"cve_releases": {
  "CVE-2024-0057": ["6.0", "7.0", "8.0"]
}
```

### 4. `release_cves`
Maps release versions to lists of CVE IDs affecting them.

Example:
```json
"release_cves": {
  "6.0": ["CVE-2024-0057", "CVE-2024-21319"],
  "7.0": ["CVE-2024-0057", "CVE-2024-21319"]
}
```

## Additional Validations

### Commits Consistency

When a `commits` dictionary exists and is non-empty, the tool validates that:
- All `products` entries have non-null, non-empty `commits` arrays
- All `packages` entries have non-null, non-empty `commits` arrays

This ensures that commit references are properly maintained when commit information is available.

## How It Works

The tool generates dictionaries by:
1. Iterating through all entries in the `products` array
2. Iterating through all entries in the `packages` array
3. Building the dictionaries based on the product/package names, CVE IDs, and release versions
4. Sorting all lists for consistency

**Note:** Products or packages with empty `release` fields are not included in the `cve_releases` and `release_cves` dictionaries.

## Building

```bash
dotnet build src/CveDictionaries/CveDictionaries.csproj
```

## Output Format

The tool generates JSON files with 2-space indentation, matching the standard format used by the .NET CVE JSON files. The formatting is handled automatically by the .NET JSON serializer with `WriteIndented = true`.

## Running

```bash
dotnet run --project src/CveDictionaries/CveDictionaries.csproj -- <command> <path>
```

Or build and run the published version:

```bash
dotnet publish src/CveDictionaries/CveDictionaries.csproj -c Release
./artifacts/bin/CveDictionaries/release/CveDictionaries <command> <path>
```
