# Timeline Naming Changes

## Summary
Updated all references from `release-history` to `timeline` and renamed enum values to use `timeline` terminology.

## Changes Made

### 1. Enum Values Updated (`DotnetRelease/Graph/ReleaseHistoryIndex.cs`)
```diff
- ReleaseHistoryIndex  → ReleaseTimelineIndex
- HistoryYearIndex     → TimelineYearIndex
- HistoryMonthIndex    → TimelineMonthIndex
```

These enum values are serialized with kebab-case-lower, resulting in JSON:
- `release-timeline-index`
- `timeline-year-index`
- `timeline-month-index`

### 2. Directory Structure
**Before:**
```
release-history/
  ├── index.json
  ├── 2024/
  │   ├── index.json
  │   └── 05/
  │       └── index.json
```

**After:**
```
timeline/
  ├── index.json
  ├── 2024/
  │   ├── index.json
  │   └── 05/
  │       └── index.json
```

### 3. Schema URIs Updated
- `schemas/dotnet-release-history-index.json` → `schemas/dotnet-release-timeline-index.json`

### 4. HAL Link Names Updated
- `"release-history"` → `"release-timeline"`
- `"release-history-{year}"` → `"release-timeline-{year}"`

### 5. Files Modified

#### ShipIndex Tool
- **ShipIndexFiles.cs**:
  - Changed output path from `release-history` to `timeline`
  - Changed input path from `release-history` to `timeline`
  - Updated enum values to use new names
  - Updated schema URIs

- **Summary.cs**:
  - Updated directory path from `release-history` to `timeline`

- **HalLinkGenerator.cs**:
  - Updated file mapping from `release-history/index.json` to `timeline/index.json`
  - Updated link name from `release-history` to `release-timeline`

- **IndexHelpers.cs**:
  - Updated mapping from `release-history` to `release-timeline`

- **Program.cs**:
  - Updated comments to reference timeline instead of history

#### VersionIndex Tool
- **ReleaseIndexFiles.cs**:
  - Updated file mapping from `release-history/index.json` to `timeline/index.json`
  - Updated year links from `release-history/{year}` to `timeline/{year}`
  - Updated link keys from `release-history` to `release-timeline`
  - Updated descriptions to use "Timeline" terminology

#### DotnetRelease Library
- **ReleaseHistoryIndex.cs**:
  - Updated enum values and descriptions
  - Updated comments to reference timeline

- **Hal.cs**:
  - Updated HAL JSON files list from `release-history/index.json` to `timeline/index.json`

## Rationale

The term "timeline" better describes the chronological organization of releases:
- More intuitive for users
- Clearer distinction from version-based indexes
- Better reflects the time-based navigation structure

## Backward Compatibility

⚠️ **Breaking Change**: 
- Directory structure changed from `release-history/` to `timeline/`
- HAL link relations changed from `release-history` to `release-timeline`
- JSON `kind` values changed (e.g., `history-year-index` → `timeline-year-index`)

Clients must update to:
1. Look for `timeline/` directory instead of `release-history/`
2. Use new link relation names
3. Handle new `kind` enum values

## Verification

✅ All projects build successfully
✅ No compilation errors or warnings
✅ Enum values properly serialized with kebab-case
✅ All path references updated consistently

## Files Changed (15 total)

1. `src/DotnetRelease/Graph/Hal.cs`
2. `src/DotnetRelease/Graph/ReleaseHistoryIndex.cs`
3. `src/ShipIndex/HalLinkGenerator.cs`
4. `src/ShipIndex/IndexHelpers.cs`
5. `src/ShipIndex/Program.cs`
6. `src/ShipIndex/ShipIndexFiles.cs`
7. `src/ShipIndex/Summary.cs`
8. `src/VersionIndex/ReleaseIndexFiles.cs`

Plus the previous CVE handling changes already committed.
