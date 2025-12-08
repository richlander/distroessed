# String Constants Implementation Summary

## Overview

All index type names and link relation strings have been centralized into constants to ensure consistency between document `kind` values and the link relation names that reference them.

The naming scheme uses **complete noun phrases** to be self-documenting and searchable.

## New Constants File

**Created:** `src/DotnetRelease/Graph/LinkRelations.cs`

This file contains all link relation constants following the refined naming pattern:
- Version-based: `{differentiator}-version-index` (e.g., `major-version-index`)
- Timeline-based: `{temporal-unit}-index` (e.g., `year-index`)

### Constants Defined

```csharp
// Version-based hierarchy (noun: version)
public const string ReleasesIndex = "releases-index";           // Root (plural)
public const string MajorVersionIndex = "major-version-index";  // Major version
public const string PatchVersionIndex = "patch-version-index";  // Patch version
public const string SdkIndex = "sdk-index";                     // SDK

// Timeline-based hierarchy (temporal nouns)
public const string TimelineIndex = "timeline-index";           // Root
public const string YearIndex = "year-index";                   // Year
public const string MonthIndex = "month-index";                 // Month

// Supplementary documents
public const string ReleaseManifest = "release-manifest";
public const string CveJson = "cve-json";
public const string Release = "release";
```

## Naming Rationale

### Why `major-version-index` not `major-index`?

**Problem with `major-index`:**
- "major" is an adjective without a noun
- Not self-documenting: major *what*?
- Ambiguous out of context

**Benefits of `major-version-index`:**
- Complete noun phrase: "major version" is the thing being indexed
- Self-documenting: immediately clear it's about a major version
- Searchable: `grep 'version-index'` finds all version-based indexes
- Natural language: matches how we'd verbally describe it

### Why `releases-index` (plural)?

- It's a collection of multiple major version releases
- Matches REST conventions (e.g., `/api/users` not `/api/user`)
- Distinguishes from individual release documents
- Provides clear distinction: `releases-index` (all) vs `major-version-index` (one major)

## Enum Updates

### ReleaseKind (src/DotnetRelease/Graph/ReleaseVersionIndex.cs)

**Updated enum values:**
- ✅ `ReleasesIndex` → serializes to `"releases-index"` (renamed from `ReleaseIndex`)
- ✅ `MajorVersionIndex` → serializes to `"major-version-index"` (renamed from `MajorIndex`)
- ✅ `PatchVersionIndex` → serializes to `"patch-version-index"` (renamed from `PatchIndex`)
- ✅ `SdkIndex` → serializes to `"sdk-index"` (no change)

**Legacy values preserved** for backwards compatibility:
- `ReleaseIndex` → `ReleasesIndex`
- `MajorIndex` → `MajorVersionIndex`
- `MajorReleaseIndex` → `MajorVersionIndex`
- `PatchIndex` → `PatchVersionIndex`
- `PatchReleaseIndex` → `PatchVersionIndex`

### HistoryKind (src/DotnetRelease/Graph/ReleaseHistoryIndex.cs)

**Updated enum values:**
- ✅ `TimelineIndex` → serializes to `"timeline-index"` (renamed from `ReleaseTimelineIndex`)
- ✅ `YearIndex` → serializes to `"year-index"` (renamed from `TimelineYearIndex`)
- ✅ `MonthIndex` → serializes to `"month-index"` (renamed from `TimelineMonthIndex`)

**Legacy values preserved** for backwards compatibility:
- `ReleaseTimelineIndex` → `TimelineIndex`
- `TimelineYearIndex` → `YearIndex`
- `TimelineMonthIndex` → `MonthIndex`

## Complete Naming Scheme

```
releases-index              # Root: all major versions
  ├─ major-version-index    # Major version: all patch releases (e.g., 8.0/index.json)
  │    ├─ patch-version-index   # Patch: specific release (e.g., 8.0.1/index.json)
  │    └─ sdk-index             # SDK: feature bands (e.g., 8.0/sdk/index.json)

timeline-index              # Root: chronological view
  ├─ year-index            # Year: all months (e.g., timeline/2024/index.json)
  │    └─ month-index          # Month: releases that month (e.g., timeline/2024/11/index.json)
```

## Search Patterns

With this naming scheme, you can easily search:

- `*-index` → all index types
- `*-version-index` → all version-based indexes
- `major-version-*` → anything related to major versions
- `timeline-*` OR `year-*` OR `month-*` → timeline-related items

The common substrings (`-version-index`, `-index`) aid discovery and maintain the "differentiator on left" pattern.

## Code Updates

### Files Updated with Constants

All hardcoded link relation strings replaced with constants from `LinkRelations`:

1. **src/ShipIndex/ShipIndexFiles.cs**
   - Line 265: `"version-index"` → `LinkRelations.MajorVersionIndex`
   - Line 498: `"release-version-index"` → `LinkRelations.ReleasesIndex`
   - Line 165: `"cve-json"` → `LinkRelations.CveJson`
   - Line 300: `"sdk-index"` → `LinkRelations.SdkIndex`

2. **src/VersionIndex/ReleaseIndexFiles.cs**
   - Line 721: `"major-version-index"` → `LinkRelations.MajorVersionIndex`
   - Line 838: `"month-timeline-index"` → `LinkRelations.MonthIndex`
   - Line 211: `"sdk-index"` → `LinkRelations.SdkIndex`
   - Line 731: `"sdk-index"` → `LinkRelations.SdkIndex`
   - Line 879: `"cve-json"` → `LinkRelations.CveJson`

3. **src/VersionIndex/HalLinkGenerator.cs**
   - Line 53: `"release-timeline-index"` → `LinkRelations.TimelineIndex`

4. **src/ShipIndex/HalLinkGenerator.cs**
   - Line 38: `"release-timeline"` → `LinkRelations.TimelineIndex`
   - Line 55: `"release-manifest"` → `LinkRelations.ReleaseManifest`

5. **src/DotnetRelease/ArchiveNavigator.cs**
   - Line 107: `"cve-json"` → `LinkRelations.CveJson`

### Enum Usage Updates

All enum references updated to use new names:

1. **src/VersionIndex/ReleaseIndexFiles.cs**
   - `ReleaseKind.ReleaseIndex` → `ReleaseKind.ReleasesIndex`
   - `ReleaseKind.MajorIndex` → `ReleaseKind.MajorVersionIndex`
   - `ReleaseKind.PatchIndex` → `ReleaseKind.PatchVersionIndex`

2. **src/ShipIndex/ShipIndexFiles.cs**
   - `HistoryKind.ReleaseTimelineIndex` → `HistoryKind.TimelineIndex`
   - `HistoryKind.TimelineYearIndex` → `HistoryKind.YearIndex`
   - `HistoryKind.TimelineMonthIndex` → `HistoryKind.MonthIndex`

## Benefits

1. **Self-documenting**: Names clearly indicate what they index (`major-version-index` vs `major-index`)
2. **Complete noun phrases**: No orphaned adjectives requiring context
3. **Type safety**: Constants catch typos at compile time
4. **Single source of truth**: All link relation names defined in one place
5. **Easy refactoring**: Change a constant value once, update everywhere
6. **Searchable**: Common substrings aid discovery (`*-version-index`)
7. **Consistency**: Guarantees kind values match link relation names
8. **Natural language**: Matches how developers verbally describe them

## Verification

✅ **Build Status**: All projects build successfully with no warnings or errors

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Generated JSON Impact

With these changes, generated JSON files will have:

### Before
```json
{
  "kind": "major-release-index",
  "_links": {
    "version-index": { ... }
  }
}
```

### After
```json
{
  "kind": "major-version-index",
  "_links": {
    "major-version-index": { ... }
  }
}
```

Perfect symmetry with complete, self-documenting names! ✨

## Next Steps

1. Run the index generators to create new JSON files with updated kind values
2. Verify generated JSON has correct link relation names
3. Update JSON schemas if they validate against specific kind values
4. Update any documentation that references old kind/link names
