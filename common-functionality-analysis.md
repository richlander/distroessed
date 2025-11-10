# Common Functionality Analysis: VersionIndex & ShipIndex

## Executive Summary

Both VersionIndex and ShipIndex tools share significant amounts of duplicated code. This analysis identifies opportunities to extract common functionality into shared libraries, improving maintainability and enforcing consistency.

## High-Priority Duplications (100% Identical)

### 1. HalJsonComparer (159 lines - EXACT DUPLICATE)
**Location:**
- `VersionIndex/HalJsonComparer.cs`
- `ShipIndex/HalJsonComparer.cs`

**Functionality:**
- Compares JSON files excluding `_metadata` property
- Determines if files need updating
- Recursively compares JSON elements

**Recommendation:** Move to shared library
```
src/IndexCommon/HalJsonComparer.cs
```

### 2. IndexHelpers (141 lines - EXACT DUPLICATE)
**Location:**
- `VersionIndex/IndexHelpers.cs`
- `ShipIndex/IndexHelpers.cs`

**Functionality:**
- Maps file types to HAL relations
- Generates HAL links for paths
- URL path generation (Prod, GitHub, CDN)
- Auxiliary file mappings

**Recommendation:** Move to shared library
```
src/IndexCommon/IndexHelpers.cs
```

### 3. LinkHelpers (23 lines - EXACT DUPLICATE)
**Location:**
- `VersionIndex/LinkHelps.cs`
- `ShipIndex/LinkHelps.cs`

**Functionality:**
- URL generation helpers
- Prod, CDN, GitHub raw, and GitHub blob path generation

**Recommendation:** Move to shared library
```
src/IndexCommon/LinkHelpers.cs
```

### 4. PathContext (3 lines - EXACT DUPLICATE)
**Location:**
- `VersionIndex/PathContext.cs`
- `ShipIndex/PathContext.cs`

**Functionality:**
- Simple record for path context with optional URL base path

**Recommendation:** Move to shared library
```
src/IndexCommon/PathContext.cs
```

### 5. Records.cs (14 lines - EXACT DUPLICATE)
**Location:**
- `VersionIndex/Records.cs`
- `ShipIndex/Records.cs`

**Functionality:**
- `ReleaseKindMapping` record
- `FileLink` record
- `LinkStyle` enum

**Recommendation:** Move to shared library
```
src/IndexCommon/Records.cs
```

### 6. Summary.cs (182 lines - NEARLY IDENTICAL)
**Location:**
- `VersionIndex/Summary.cs` (185 lines)
- `ShipIndex/Summary.cs` (182 lines)

**Differences:**
- Line 79 in VersionIndex: `var label = sdk.Version ?? $".NET SDK {version}";`
- Line 77 in ShipIndex: `var label = $".NET SDK {version}";`
- Different using statements (VersionIndex includes `DotnetRelease.ReleaseInfo`)

**Functionality:**
- `GetReleaseSummariesAsync` - Scans directories for major/patch releases
- `GetReleaseCalendar` - Organizes releases by date
- `PopulateCveInformation` - Links CVE data to releases

**Recommendation:** Move to shared library with the fix from ShipIndex
```
src/IndexCommon/Summary.cs
```

## Medium-Priority Duplications (Similar with Variations)

### 7. HalLinkGenerator (109 vs 90 lines - SIMILAR)
**Location:**
- `VersionIndex/HalLinkGenerator.cs` (109 lines)
- `ShipIndex/HalLinkGenerator.cs` (90 lines)

**Key Differences:**
- VersionIndex handles parent directory files (`../llms/` paths) - lines 33-43
- VersionIndex has more semantic mappings (quick-ref, README, usage) - lines 50-78
- ShipIndex is simpler, no parent directory handling

**Functionality:**
- Generates HAL links from file lists
- Maps filenames to semantic relations
- Handles multiple link styles (Prod, GitHub)

**Recommendation:** Move to shared library with configuration for:
- Parent directory handling (optional)
- Semantic mapping rules (configurable)
```
src/IndexCommon/HalLinkGenerator.cs
src/IndexCommon/HalLinkGeneratorConfig.cs
```

## Shared Dependencies Already in DotnetRelease Library

Both tools already share:
- `DotnetRelease.Graph.HalLink`
- `DotnetRelease.Graph.HalTerms`
- `DotnetRelease.Graph.MediaType`
- `DotnetRelease.Graph.ReleaseKind`
- `DotnetRelease.Summary.*` records
- `DotnetRelease.Security.*` records

## Proposed Shared Library Structure

```
src/IndexCommon/
├── IndexCommon.csproj
├── HalJsonComparer.cs          # File comparison with metadata exclusion
├── HalLinkGenerator.cs         # Configurable HAL link generation
├── HalLinkGeneratorConfig.cs   # Configuration for link generation
├── IndexHelpers.cs             # HAL link generation helpers
├── LinkHelpers.cs              # URL generation utilities
├── PathContext.cs              # Path context record
├── Records.cs                  # Common records (FileLink, etc.)
├── Summary.cs                  # Release summary extraction
└── README.md                   # Documentation
```

## Implementation Plan

### Phase 1: Extract Exact Duplicates (Low Risk)
1. Create `src/IndexCommon/` project
2. Move exact duplicates:
   - HalJsonComparer.cs
   - IndexHelpers.cs
   - LinkHelpers.cs
   - PathContext.cs
   - Records.cs
   - Summary.cs (with ShipIndex's fix)
3. Update VersionIndex.csproj and ShipIndex.csproj
4. Remove duplicated files
5. Fix namespace references
6. Run tests

### Phase 2: Unify HalLinkGenerator (Medium Risk)
1. Design configuration system for variations
2. Create unified HalLinkGenerator with config
3. Update both tools to use configured generator
4. Test both tools thoroughly

### Phase 3: Extract CVE Handling (from previous analysis)
1. Create `src/CveHandler/` library
2. Implement common CVE processing
3. Standardize on `disclosures` format
4. Generate patch-level indexes

### Phase 4: Identify Additional Opportunities
1. Schema injection logic
2. Glossary/Usage generation
3. Version range calculation
4. File writing with skipped count tracking

## Benefits

### Maintainability
- Single source of truth for common logic
- Bug fixes propagate to both tools
- Easier to review changes

### Consistency
- Enforced behavior across tools
- Identical HAL link generation
- Consistent file comparison logic

### Testing
- Test once, use everywhere
- Shared test fixtures
- Reduced test duplication

### Future Tools
- New index generators can reuse infrastructure
- Faster development of new tools
- Consistent patterns across tooling

## Code Metrics

| Component | VersionIndex | ShipIndex | Status |
|-----------|-------------|-----------|--------|
| HalJsonComparer | 159 lines | 159 lines | ✅ Identical |
| IndexHelpers | 141 lines | 141 lines | ✅ Identical |
| LinkHelpers | 23 lines | 23 lines | ✅ Identical |
| PathContext | 3 lines | 3 lines | ✅ Identical |
| Records.cs | 14 lines | 14 lines | ✅ Identical |
| Summary.cs | 185 lines | 182 lines | ⚠️ Nearly identical (1 line diff) |
| HalLinkGenerator | 109 lines | 90 lines | 🔄 Similar (needs unification) |

**Total Exact Duplication:** ~520 lines  
**Total Similar Code:** ~200 lines  
**Potential Extraction:** ~720 lines

## Migration Checklist

### Pre-Migration
- [ ] Review and document current behavior
- [ ] Identify test coverage gaps
- [ ] Create IndexCommon project
- [ ] Set up project references

### Migration Execution
- [ ] Move HalJsonComparer
- [ ] Move IndexHelpers
- [ ] Move LinkHelpers
- [ ] Move PathContext
- [ ] Move Records.cs
- [ ] Move Summary.cs
- [ ] Unify HalLinkGenerator

### Post-Migration
- [ ] Update all namespace references
- [ ] Run full test suite for both tools
- [ ] Update documentation
- [ ] Remove old files
- [ ] Verify build succeeds
- [ ] Test end-to-end scenarios

## Additional Observations

### File Mappings
Both tools define similar file mapping dictionaries:
- `MainFileMappings` / `HistoryFileMappings`
- `PatchFileMappings`
- `AuxFileMappings`

These could be standardized and shared, with tool-specific extensions.

### Glossary Generation
Both tools create similar glossary dictionaries. This could be centralized:
```csharp
public static class StandardGlossary
{
    public static Dictionary<string, string> GetReleaseTerms() { ... }
    public static Dictionary<string, string> GetSecurityTerms() { ... }
}
```

### Metadata Generation
Both tools create `GenerationMetadata` with timestamp. This could be a shared utility:
```csharp
public static class MetadataGenerator
{
    public static GenerationMetadata Create(string toolName) 
        => new("1.0", DateTimeOffset.UtcNow, toolName);
}
```

## Risk Assessment

| Component | Risk Level | Rationale |
|-----------|-----------|-----------|
| HalJsonComparer | Low | Pure utility, no state |
| IndexHelpers | Low | Pure functions |
| LinkHelpers | Low | Pure functions |
| PathContext | Low | Simple record |
| Records.cs | Low | Data structures only |
| Summary.cs | Medium | Complex logic, needs testing |
| HalLinkGenerator | Medium | Variations between tools |

## Recommendations Priority

1. **High Priority (Do Now):**
   - Extract exact duplicates (Phase 1)
   - Creates immediate value with minimal risk
   
2. **Medium Priority (Next Sprint):**
   - Unify HalLinkGenerator (Phase 2)
   - Extract CVE handling (Phase 3)
   
3. **Low Priority (Future):**
   - Additional utilities (Phase 4)
   - Can be done incrementally

## Success Metrics

- **Code Reduction:** Remove ~700+ lines of duplication
- **Build Time:** Should remain same or improve
- **Test Coverage:** Should increase with shared tests
- **Bug Rate:** Should decrease with single source of truth
- **Development Speed:** Faster for new features

## Next Steps

1. Review this analysis with team
2. Get approval for Phase 1 extraction
3. Create `src/IndexCommon/` project
4. Begin migration of exact duplicates
5. Schedule regular progress reviews
