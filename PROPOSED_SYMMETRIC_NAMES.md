# Proposed Symmetric Naming Scheme

## Naming Pattern: `{kind}-index`

Following the pattern where the differentiator is on the left and "index" is always on the right for easy searching.

---

## Current vs Proposed Names

### Version-Based Indexes (Hierarchical: All → Major → Patch)

| Current Kind | Current Link Relations | **Proposed Name** | Notes |
|-------------|----------------------|------------------|-------|
| `release-index` | `release-version-index`, `release-index` | `releases-index` | Plural for collection |
| `major-release-index` | `version-index`, `major-version-index` | `major-version-index` | Complete noun phrase |
| `patch-release-index` | `patch-release-index` | `patch-version-index` | Complete noun phrase |

### Timeline-Based Indexes (Chronological: All → Year → Month)

| Current Kind | Current Link Relations | **Proposed Name** | Notes |
|-------------|----------------------|------------------|-------|
| `release-timeline-index` | `release-timeline-index` | `timeline-index` | Simpler (no "release" redundancy) |
| `timeline-year-index` | N/A | `year-index` | Simpler, clearer hierarchy |
| `timeline-month-index` | `month-timeline-index` | `month-index` | Simpler, clearer hierarchy |

### SDK Indexes (Feature Band Hierarchy)

| Current (Implicit) | **Proposed Name** | Notes |
|-------------------|------------------|-------|
| N/A | `sdk-index` | Root SDK index per major version |
| N/A | `sdk-band-index` | Individual feature band (if needed) |

---

## Proposed Complete Hierarchy

```
releases-index              (root: all major versions)
├── major-version-index     (e.g., 8.0/index.json - all patches)
│   ├── patch-version-index (e.g., 8.0.1/index.json - patch details)
│   └── sdk-index           (e.g., 8.0/sdk/index.json - all SDK bands)
│
timeline-index              (root: all years)
├── year-index              (e.g., timeline/2024/index.json - all months)
│   └── month-index         (e.g., timeline/2024/11/index.json - releases)
```

---

## Benefits of This Scheme

1. **Left-aligned differentiator**: `releases-`, `major-version-`, `patch-version-`, `timeline-`, `year-`, `month-`
2. **Consistent suffix**: All end in `-index`
3. **Easy to search**: `grep "*-index"` finds all index types; `grep "*-version-index"` finds version-based
4. **Intuitive hierarchy**: 
   - Version: `releases` → `major-version` → `patch-version`
   - Timeline: `timeline` → `year` → `month`
5. **No redundancy**: Removed "release" from timeline names
6. **Complete noun phrases**: No orphaned adjectives (`major-version` not just `major`)
7. **Self-documenting**: Each name clearly indicates its level and what it indexes

---

## Examples in Context

### Version Index Chain
```json
// releases-index (root)
{
  "kind": "releases-index",
  "_links": {
    "self": { "href": ".../index.json" }
  },
  "_embedded": {
    "releases": [
      {
        "version": "8.0",
        "_links": {
          "major-version-index": { "href": ".../8.0/index.json" }
        }
      }
    ]
  }
}

// major-version-index (8.0/index.json)
{
  "kind": "major-version-index",
  "_links": {
    "self": { "href": ".../8.0/index.json" },
    "sdk-index": { "href": ".../8.0/sdk/index.json" }
  },
  "_embedded": {
    "releases": [
      {
        "version": "8.0.1",
        "_links": {
          "patch-version-index": { "href": ".../8.0.1/index.json" }
        }
      }
    ]
  }
}

// patch-version-index (8.0.1/index.json)
{
  "kind": "patch-version-index",
  "_links": {
    "self": { "href": ".../8.0.1/index.json" },
    "major-version-index": { "href": ".../8.0/index.json" }
  }
}
```

### Timeline Index Chain
```json
// timeline-index (root)
{
  "kind": "timeline-index",
  "_links": {
    "self": { "href": ".../timeline/index.json" },
    "release-index": { "href": ".../index.json" }
  },
  "_embedded": {
    "years": [
      {
        "year": "2024",
        "_links": {
          "year-index": { "href": ".../timeline/2024/index.json" }
        }
      }
    ]
  }
}

// year-index (timeline/2024/index.json)
{
  "kind": "year-index",
  "_links": {
    "self": { "href": ".../timeline/2024/index.json" }
  },
  "_embedded": {
    "months": [
      {
        "month": "11",
        "_links": {
          "month-index": { "href": ".../timeline/2024/11/index.json" }
        }
      }
    ]
  }
}

// month-index (timeline/2024/11/index.json)
{
  "kind": "month-index",
  "_links": {
    "self": { "href": ".../timeline/2024/11/index.json" },
    "year-index": { "href": ".../timeline/2024/index.json" }
  },
  "_embedded": {
    "releases": {
      "9.0": {
        "_links": {
          "major-version-index": { "href": ".../9.0/index.json" }
        }
      }
    }
  }
}
```

---

## Alternative: Keep Some "Context" Words?

If we want to preserve context in standalone usage:

| Type | Minimal | With Context |
|------|---------|--------------|
| Root release | `releases-index` | `releases-index` ✓ |
| Major version | `major-version-index` | `major-version-index` ✓ |
| Patch release | `patch-version-index` | `patch-version-index` ✓ |
| Root timeline | `timeline-index` | `timeline-index` ✓ |
| Timeline year | `year-index` | `year-index` ✓ |
| Timeline month | `month-index` | `month-index` ✓ |

**Recommendation**: Use these names as-is. Complete noun phrases provide necessary context.

---

## Summary Table: All Changes

| Document Type | Current Kind | Current Links | **Proposed** |
|--------------|-------------|---------------|-------------|
| Root version index | `release-index` | `release-version-index`, `release-index` | `releases-index` |
| Major version index | `major-release-index` | `version-index`, `major-version-index` | `major-version-index` |
| Patch detail index | `patch-release-index` | N/A | `patch-version-index` |
| Root timeline index | `release-timeline-index` | `release-timeline-index` | `timeline-index` |
| Year timeline index | `timeline-year-index` | N/A | `year-index` |
| Month timeline index | `timeline-month-index` | `month-timeline-index` | `month-index` |
| SDK index | N/A | `sdk-index` | `sdk-index` ✓ |

---

## Implementation Impact

### Enum Updates Required

**ReleaseKind** (src/DotnetRelease/Graph/ReleaseVersionIndex.cs):
```csharp
public enum ReleaseKind
{
    ReleasesIndex,           // releases-index (was ReleaseIndex)
    MajorVersionIndex,       // major-version-index (was MajorReleaseIndex)
    PatchVersionIndex,       // patch-version-index (was PatchReleaseIndex)
    SdkIndex,                // sdk-index (new)
}
```

**HistoryKind** (src/DotnetRelease/Graph/ReleaseHistoryIndex.cs):
```csharp
public enum HistoryKind
{
    TimelineIndex,           // timeline-index (was ReleaseTimelineIndex)
    YearIndex,               // year-index (was TimelineYearIndex)
    MonthIndex,              // month-index (was TimelineMonthIndex)
}
```

### Link Relation Updates Required

**ShipIndex** (src/ShipIndex/ShipIndexFiles.cs):
- Line 265: `"version-index"` → `"major-version-index"`
- Line 498: `"release-version-index"` → `"releases-index"`

**VersionIndex** (src/VersionIndex/ReleaseIndexFiles.cs):
- Line 721: `"major-version-index"` → `"major-version-index"` (already correct!)
- Line 838: `"month-timeline-index"` → `"month-index"`

---

## Recommendation

**Use complete noun phrases**: `releases-index`, `major-version-index`, `patch-version-index`, `timeline-index`, `year-index`, `month-index`

This provides:
- Maximum clarity and self-documentation
- Complete noun phrases (no orphaned adjectives)
- Perfect symmetry across hierarchies
- Easy searchability with `grep '*-index'` or `grep '*-version-index'`
- Clear hierarchy (differentiator on left, base noun in middle, type suffix on right)
- No ambiguity about what's being indexed
