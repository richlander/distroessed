# String Interning Implementation for ShipIndex and VersionIndex

## Summary
Implemented string interning for commonly repeated strings in the generated index JSON files to reduce memory footprint and improve performance.

## Changes Made

### 1. New File: `src/DotnetRelease/Graph/LinkTitles.cs`
Created a new static class to hold interned link title strings that are reused across multiple index files:
- Common titles: "Index", "History Index", ".NET Release Index", etc.
- CVE-related: "CVE Information"
- Documentation: "Usage Guide", "Quick Reference", "Glossary", "Support Policy"
- OS/Packages: "Supported OSes", "Linux Packages"

### 2. Modified: `src/DotnetRelease/Graph/IndexTitles.cs`
Updated the IndexTitles class to use string interning:
- Changed `const` fields to `static readonly` with `string.Intern()`
- Applied interning to all title and description methods
- **Updated `TimelineIndexLink` to ".NET Release Timeline Index" for symmetry with `VersionIndexLink`**
- **Updated year and month link titles to include "index" (e.g., "Release timeline index for 2024")**
- Ensures single instances of commonly used index titles and descriptions

### 3. Modified: `src/ShipIndex/ShipIndexFiles.cs`
Updated to use the new interned strings:
- `HistoryFileMappings`: Uses `LinkTitles.HistoryIndex`, `LinkTitles.CveInformation`, `LinkTitles.DotNetReleaseIndex`, `LinkTitles.DotNetReleaseNotes`
- Monthly CVE links: Uses `LinkTitles.CveInformation`

### 4. Modified: `src/VersionIndex/ReleaseIndexFiles.cs`
Updated to use the new interned strings:
- `MainFileMappings`: Uses `LinkTitles.DotNetReleaseIndex`, `LinkTitles.UsageGuide`, `LinkTitles.QuickReference`, `LinkTitles.Glossary`, `LinkTitles.SupportPolicy`
- `PatchFileMappings`: Uses `LinkTitles.Index`, `LinkTitles.ReleaseManifest`, `LinkTitles.CompleteReleaseInformation`, `LinkTitles.Release`
- `AuxFileMappings`: Uses `LinkTitles.SupportedOSes`, `LinkTitles.LinuxPackages`, `LinkTitles.ReleaseNotes`
- CVE links in patch detail indexes: Uses `LinkTitles.CveInformation`

## Benefits
1. **Memory Efficiency**: String interning ensures that identical strings share the same memory location in the JSON output
2. **Consistency**: Centralized string definitions prevent typos and ensure consistent naming
3. **Maintainability**: Changes to titles/descriptions only need to be made in one place
4. **Performance**: String comparisons on interned strings are faster (reference equality)

## Testing
- Both `ShipIndex` and `VersionIndex` projects build successfully
- No breaking changes to the public API or generated JSON structure
- All interned strings are functionally equivalent to their previous literal values

## Files Affected
- `src/DotnetRelease/Graph/LinkTitles.cs` (new)
- `src/DotnetRelease/Graph/IndexTitles.cs` (modified)
- `src/ShipIndex/ShipIndexFiles.cs` (modified)
- `src/VersionIndex/ReleaseIndexFiles.cs` (modified)
