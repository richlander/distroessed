# MsrcValidate

Tool to validate and update CVE data against authoritative MSRC (Microsoft Security Response Center) sources.

## Usage

```bash
# Validate mode (read-only)
dotnet run --project src/MsrcValidate validate <path-to-cve.json>

# Update mode (modifies files)
dotnet run --project src/MsrcValidate update <path-to-cve.json>
```

## What It Validates

The tool fetches authoritative CVE data from the MSRC CVRF API and validates/updates:

### CVSS Fields
- **Score**: CVSS base score (decimal)
- **Vector**: CVSS vector string
- **Severity**: CVSS severity rating derived from score (low/medium/high/critical)

### CVE Metadata Fields
- **Impact**: Microsoft's impact classification (e.g., "Elevation of Privilege", "Security Feature Bypass")
- **Weakness**: CWE (Common Weakness Enumeration) identifier (e.g., "CWE-20")

### CNA Fields
- **Severity**: CNA-specific severity rating (e.g., Microsoft's "Important", "Critical") - stored in the CNA object

## Schema Changes

This tool required the following schema extensions to `CveRecords.cs`:

### 1. CVSS Record
Extended with severity rating:
```csharp
public record Cvss(
    string Version,
    string Vector,
    decimal Score = 0.0m,     // New field
    string Severity = ""      // New field
);
```

### 2. CVE Record
Added `Impact` and `Weakness` fields:
```csharp
public record Cve(
    // ... existing fields ...
    string Impact = "",      // New: Microsoft impact classification
    string? Weakness = null  // New: CWE identifier
);
```

### 3. CNA Record
Converted from string to object to allow CNA-specific extensions:
```csharp
// Old: string Cna
// New:
public record Cna(
    string Name,
    string? Severity = null  // CNA-specific severity rating
);
```

The CNA converter handles backward compatibility with the legacy string format.

## Example Output

```
Processing: /path/to/2023/11/cve.json
  Fetching MSRC data for 2023-Nov...
  [CVE-2023-36049] Score mismatch: 0.0 (current) vs 7.6 (MSRC)
  [CVE-2023-36049] Vector mismatch: ... (current) vs ... (MSRC)
  [CVE-2023-36049] Impact mismatch:  (current) vs Elevation of Privilege (MSRC)
  [CVE-2023-36049] Weakness mismatch: (null) (current) vs CWE-20 (MSRC)
  [CVE-2023-36049] CNA Severity mismatch: (null) (current) vs Important (MSRC)
  [CVE-2023-36049] CVSS Severity mismatch: critical (current) vs high (expected for score 7.6)
  Updated /path/to/2023/11/cve.json

Updated 1 file(s), found 16 issue(s).
```

## Data Sources

- **MSRC CVRF API**: `https://api.msrc.microsoft.com/cvrf/v2.0/cvrf/{YYYY-MMM}`
- Parses CVRF XML format including embedded HTML tables and CVSS score sets
- Extracts Impact, Severity, and CWE from Vulnerability/Threat elements

## Notes

- **CVSS Severity vs CNA Severity**: These are different concepts
  - CVSS Severity: Standardized rating derived from CVSS score (stored in `cvss.severity`)
  - CNA Severity: CNA's own severity rating (stored in `cna.severity`)
  - Example: A CVE with CVSS score 7.6 has CVSS severity "high" but Microsoft's CNA severity "Important"
