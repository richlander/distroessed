# Complete Label Asymmetry Catalog

## Summary

The main issue is that **kind values** (document type identifiers) don't match the **link relation names** (HAL+JSON relation keys) that point to those documents. This creates inconsistency in the hypermedia API.

## Key Principle

**For consistency:** When document A has `"kind": "X"`, any link pointing to document A should use link relation `"X"`.

---

## Detailed Asymmetries Found

### 1. Major Release Index (CRITICAL)
**Document:** `6.0/index.json`, `8.0/index.json`, etc.
- **Kind value:** `"major-release-index"` ✓ (from `ReleaseKind.MajorReleaseIndex`)
- **Link relation in ShipIndex:** `"version-index"` ❌ (line 265)
- **Link relation in VersionIndex:** `"major-version-index"` ❌ (line 721)
- **Should be:** `"major-release-index"`

**Locations to fix:**
- `src/ShipIndex/ShipIndexFiles.cs:265` - Change `"version-index"` to `"major-release-index"`
- `src/VersionIndex/ReleaseIndexFiles.cs:721` - Change `"major-version-index"` to `"major-release-index"`

---

### 2. Root Release Version Index
**Document:** `index.json` (root)
- **Kind value:** `"release-index"` ✓ (from `ReleaseKind.ReleaseIndex`)
- **Link relation from timeline back to root:** `"release-version-index"` ❌ (ShipIndex line 498)
- **Should be:** `"release-index"`

**Location to fix:**
- `src/ShipIndex/ShipIndexFiles.cs:498` - Change `"release-version-index"` to `"release-index"`

---

### 3. Timeline Month Index
**Document:** `timeline/2017/03/index.json`, etc.
- **Kind value:** `"timeline-month-index"` ✓ (from `HistoryKind.TimelineMonthIndex`)
- **Link relation in VersionIndex:** `"month-timeline-index"` ❌ (line 838)
- **Should be:** `"timeline-month-index"`

**Location to fix:**
- `src/VersionIndex/ReleaseIndexFiles.cs:838` - Change `"month-timeline-index"` to `"timeline-month-index"`

---

### 4. Root Timeline Index (CORRECT ✓)
**Document:** `timeline/index.json`
- **Kind value:** `"release-timeline-index"` ✓
- **Link relation in VersionIndex:** `"release-timeline-index"` ✓ (HalLinkGenerator line 53)
- **Status:** Already correct!

---

## Enum Definitions Reference

### ReleaseKind (src/DotnetRelease/Graph/ReleaseVersionIndex.cs)
Serializes to kebab-case-lower:
```
ReleaseIndex           → "release-index"
MajorReleaseIndex      → "major-release-index"  
PatchReleaseIndex      → "patch-release-index"
```

### HistoryKind (src/DotnetRelease/Graph/ReleaseHistoryIndex.cs)
Serializes to kebab-case-lower:
```
ReleaseTimelineIndex   → "release-timeline-index"
TimelineYearIndex      → "timeline-year-index"
TimelineMonthIndex     → "timeline-month-index"
```

---

## Files That Need Changes

1. **src/ShipIndex/ShipIndexFiles.cs**
   - Line 265: `"version-index"` → `"major-release-index"`
   - Line 498: `"release-version-index"` → `"release-index"`

2. **src/VersionIndex/ReleaseIndexFiles.cs**
   - Line 721: `"major-version-index"` → `"major-release-index"`
   - Line 838: `"month-timeline-index"` → `"timeline-month-index"`

---

## Impact Analysis

These changes will:
- Align link relation names with document kind values
- Make the hypermedia API more consistent and predictable
- Allow clients to discover resource types from link relations
- Follow RESTful hypermedia best practices (HATEOAS)

## Testing Required

After changes, verify:
1. Generated JSON files have correct link relation names
2. All cross-references between version and timeline indexes are correct
3. No other code depends on the old link relation names

---

## Examples from Actual Files

### Example 1: Major Release Index Mismatch
From `/Users/rich/git/core/release-notes/6.0/index.json`:
```json
{
  "kind": "major-release-index",
  ...
}
```

From `/Users/rich/git/core/release-notes/timeline/2017/03/index.json`:
```json
{
  "_links": {
    "version-index": {
      "href": "...",
      "title": ".NET 1.1 Version Index"
    }
  }
}
```

**Problem:** Link relation is `"version-index"` but document kind is `"major-release-index"`.

### Example 2: Root Index Mismatch
From `/Users/rich/git/core/release-notes/index.json`:
```json
{
  "kind": "release-index",
  ...
}
```

From `/Users/rich/git/core/release-notes/timeline/index.json`:
```json
{
  "_links": {
    "release-version-index": {
      "href": "...",
      "title": ".NET Release Index"
    }
  }
}
```

**Problem:** Link relation is `"release-version-index"` but document kind is `"release-index"`.
