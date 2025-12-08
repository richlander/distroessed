# CVE Handling Changes - Complete Summary

## Objective
Implement symmetric CVE handling between ShipIndex and VersionIndex tools as described in `cve-handling-analysis.md`.

## Key Changes

### 1. New CveHandler Library
**Location**: `src/CveHandler/`

Created a shared library to eliminate code duplication:
- **CveLoader.cs**: Loads CVE data from JSON files
- **CveTransformer.cs**: Converts between full disclosures and summaries
- **README.md**: Documentation for the library

**Benefits**:
- Single source of truth for CVE processing
- Consistent behavior across tools
- Easier to maintain and extend

### 2. Updated Data Structures
**Location**: `src/DotnetRelease/Graph/`

#### New:
- **PatchDetailIndex.cs**: Structure for patch-level detail indexes with CVE disclosures

#### Modified:
- **PatchReleaseVersionIndex.cs**: 
  - Added `CveRecords` property (CVE IDs) at major version level
  
- **PatchReleaseVersionIndexEntry.cs**: 
  - Changed `CveRecords` from `IReadOnlyList<CveRecordSummary>` to `IReadOnlyList<string>`
  
- **ReleaseVersionIndexEntry.cs** (legacy):
  - Changed `CveRecords` to `IReadOnlyList<string>`
  
- **HalJsonSerializationContext.cs**: 
  - Added `PatchDetailIndex` to serialization context

- **PatchSummary.cs**: 
  - Updated to reflect CVE IDs instead of full records

### 3. VersionIndex Tool Updates
**Location**: `src/VersionIndex/`

**ReleaseIndexFiles.cs**:
- Changed `GetPatchIndexEntries()` to `GetPatchIndexEntriesAsync()`:
  - Now extracts CVE IDs instead of full summaries
  - Generates patch-level detail index files
  - Adds links to patch detail indexes
  
- Added `GeneratePatchDetailIndexAsync()`:
  - Loads CVE data for major versions
  - Filters by patch release
  - Writes `{major}/{patch}/index.json` files with full CVE disclosures
  
- Updated major version index generation:
  - Uses `PatchReleaseVersionIndex` instead of `ReleaseVersionIndex`
  - Collects all CVE IDs from patches at major version level

**VersionIndex.csproj**:
- Added reference to CveHandler library

### 4. ShipIndex Tool Updates
**Location**: `src/ShipIndex/`

**ShipIndexFiles.cs**:
- Replaced inline CVE transformation with `CveHandler.CveTransformer.ToSummaries()`
- Uses `CveHandler.CveLoader` for loading CVE data
- Removed ~70 lines of duplicate CVE transformation code

**ShipIndex.csproj**:
- Added reference to CveHandler library

### 5. Solution Updates
**src.sln**:
- Added CveHandler project to solution

## Architecture Changes

### Before:
```
VersionIndex: CVE summaries at patch level (in major version index)
ShipIndex: CVE disclosures at month level (separate from year index)
```

### After:
```
Both tools follow same pattern:
- Summary level (major/year): CVE IDs only
- Detail level (patch/month): Full CVE disclosures in separate index files
```

## File Structure Changes

### VersionIndex Output
**Before**:
```
9.0/
  └── index.json          # Patch list with CVE summaries embedded
```

**After**:
```
9.0/
  ├── index.json          # Patch list with CVE IDs + major version CVE IDs
  ├── 9.0.0/
  │   └── index.json      # NEW: Full CVE disclosures for this patch
  └── 9.0.1/
      └── index.json      # NEW: Full CVE disclosures for this patch
```

### ShipIndex Output (unchanged structure, improved code)
```
release-history/
  └── 2024/
      ├── index.json      # Year with month summaries
      └── 05/
          └── index.json  # Month with CVE disclosures
```

## Breaking Changes

⚠️ **CVE Records Format Change**:
- `PatchReleaseVersionIndexEntry.CveRecords` changed from `IReadOnlyList<CveRecordSummary>` to `IReadOnlyList<string>`
- Clients expecting full CVE objects will need to fetch from patch-level detail indexes

## Migration Path for Clients

1. **For CVE IDs**: Continue reading from major/patch index `cve-records` property
2. **For Full CVE Details**: 
   - Fetch from new `{major}/{patch}/index.json` files
   - Or continue using existing month-level indexes from ShipIndex

## Verification

✅ All projects build successfully:
- CveHandler library compiles
- DotnetRelease with updated structures compiles
- VersionIndex with new generation logic compiles
- ShipIndex with shared handler compiles
- Complete solution builds without errors or warnings

✅ Code reduction:
- Removed ~70 lines of duplicate CVE transformation code from ShipIndex
- Centralized CVE handling in shared library

✅ Symmetry achieved:
- Both tools use same CVE format (`disclosures`)
- Both tools have same graph depth (summary → detail)
- Both tools use shared transformation logic

## Files Changed

### Modified (9 files):
1. `src/DotnetRelease/Graph/HalJsonSerializationContext.cs`
2. `src/DotnetRelease/Graph/PatchReleaseVersionIndex.cs`
3. `src/DotnetRelease/Graph/ReleaseVersionIndex.cs`
4. `src/DotnetRelease/PatchSummary.cs`
5. `src/ShipIndex/ShipIndex.csproj`
6. `src/ShipIndex/ShipIndexFiles.cs`
7. `src/VersionIndex/ReleaseIndexFiles.cs`
8. `src/VersionIndex/VersionIndex.csproj`
9. `src/src.sln`

### Created (6 files):
1. `src/CveHandler/CveHandler.csproj`
2. `src/CveHandler/CveLoader.cs`
3. `src/CveHandler/CveTransformer.cs`
4. `src/CveHandler/README.md`
5. `src/DotnetRelease/Graph/PatchDetailIndex.cs`
6. `cve-handling-implementation.md`

### Documentation:
1. `cve-handling-analysis.md` - Original analysis (existing)
2. `cve-handling-implementation.md` - Implementation summary (new)
3. `src/CveHandler/README.md` - Library documentation (new)

## Next Steps

1. ✅ Implementation complete
2. Test with actual data to verify patch-level indexes generate correctly
3. Update JSON schemas if needed for new structures
4. Update documentation for API consumers about CVE format changes
5. Consider adding integration tests for CVE handling

## Notes

- All changes follow the plan outlined in `cve-handling-analysis.md`
- Code is backward compatible except for CVE record format change
- Performance should improve due to reduced code duplication
- Maintenance burden reduced with shared library
