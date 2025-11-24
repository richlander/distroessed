# Comprehensive Latest Links Strategy - Final

## Overview

A complete "latest" link strategy across all index types with:
1. **Context-dependent naming**: Use `latest` alone when context provides the noun
2. **Qualified naming**: Use `latest-X` for subsets, cross-references, or disambiguation
3. **Properties before `_links`**: Scalar values appear first for quick scanning
4. **Cross-advertising**: Root-level bidirectional links between hierarchies
5. **Cache-friendly**: No multi-level "latest" chains

## Naming Principles

### Use `latest` (Unqualified)

When it means "most recent item in THIS collection":

```json
{
  "kind": "major-version-index",  // Context provides noun phrase
  "latest": "8.0.11"              // = "latest [major-version]"
}
```

**Rationale:** The document's `kind`, `title`, and URL provide complete context. Follows REST conventions.

### Use `latest-X` (Qualified)

Only when:
1. **Subset filter**: Filtering the primary collection (e.g., `latest-lts`, `latest-security`)
2. **Cross-reference**: Pointing to different hierarchy (e.g., `latest-year` from releases)
3. **Disambiguation**: Avoiding name conflicts (e.g., `latest-year` vs version `latest`)

---

## Latest Links by Index Type

### 1. releases-index (Root Version Index)

**Properties (before `_links`):**
```json
{
  "kind": "releases-index",
  "title": ".NET Release Index",
  "description": "...",
  
  "latest": "10.0",              // Latest major version (primary)
  "latest-lts": "8.0",           // Latest LTS major (subset filter)
  "latest-year": "2025",         // Cross-reference to timeline
  
  "_links": {
    "self": { "href": ".../index.json" },
    "timeline-index": { "href": ".../timeline/index.json" },
    
    "latest": { 
      "href": ".../10.0/index.json",
      "title": "Latest .NET release (.NET 10.0)"
    },
    "latest-lts": { 
      "href": ".../8.0/index.json",
      "title": "Latest LTS release (.NET 8.0)"
    },
    "latest-sdk": { 
      "href": ".../10.0/sdk/index.json",
      "title": "Latest .NET SDK (10.0)"
    },
    "latest-year": { 
      "href": ".../timeline/2025/index.json",
      "title": "Latest year (2025)"
    }
  }
}
```

**Notes:**
- `latest` = primary (latest major)
- `latest-lts` = subset filter (LTS majors only)
- `latest-sdk` = link only, no property (derived from `latest`)
- `latest-year` = cross-reference to timeline hierarchy

---

### 2. major-version-index (e.g., 8.0/index.json)

**Properties (before `_links`):**
```json
{
  "kind": "major-version-index",
  "title": ".NET 8.0 Patch Release Index",
  "description": "...",
  
  "latest": "8.0.11",            // Latest patch (primary)
  "latest-security": "8.0.10",   // Latest patch with CVE fixes (subset filter)
  
  "_links": {
    "self": { "href": ".../8.0/index.json" },
    "releases-index": { "href": ".../index.json" },
    "sdk-index": { "href": ".../8.0/sdk/index.json" },
    
    "latest": { 
      "href": ".../8.0/8.0.11/index.json",
      "title": "Latest patch release (8.0.11)"
    },
    "latest-security": { 
      "href": ".../8.0/8.0.10/index.json",
      "title": "Latest security patch (8.0.10)"
    }
  }
}
```

**Notes:**
- `latest` = primary (latest patch in this major)
- `latest-security` = subset filter (patches with CVE fixes)
- No cross-links at this depth (stay focused)

---

### 3. patch-version-index (e.g., 8.0.1/index.json)

**Properties: None** (this IS a specific patch - leaf node)

```json
{
  "kind": "patch-version-index",
  "version": "8.0.1",
  "_links": {
    "self": { "href": ".../8.0/8.0.1/index.json" },
    "major-version-index": { "href": ".../8.0/index.json" }
  }
}
```

**Notes:**
- Leaf node - no "latest" applicable
- Navigate up only

---

### 4. sdk-index (e.g., 8.0/sdk/index.json)

**Properties (before `_links`):**
```json
{
  "kind": "sdk-index",
  "title": ".NET SDK 8.0 Release Information",
  "description": "...",
  
  "latest": "8.0.4xx",           // Latest SDK feature band (primary)
  
  "_links": {
    "self": { "href": ".../8.0/sdk/index.json" },
    "major-version-index": { "href": ".../8.0/index.json" },
    
    "latest": { 
      "href": ".../8.0/sdk/sdk-8.0.4xx.json",
      "title": "Latest SDK feature band (8.0.4xx)"
    }
  }
}
```

**Notes:**
- `latest` = primary (latest feature band in this SDK version)
- Context clear: SDK index contains bands

---

### 5. timeline-index (Root Chronological)

**Properties (before `_links`):**
```json
{
  "kind": "timeline-index",
  "title": ".NET Release Timeline Index",
  "description": "...",
  
  "latest-year": "2025",         // Latest year (primary - qualified for clarity)
  "latest": "10.0",              // Cross-reference: latest version
  "latest-lts": "8.0",           // Cross-reference: latest LTS
  
  "_links": {
    "self": { "href": ".../timeline/index.json" },
    "releases-index": { "href": ".../index.json" },
    
    "latest-year": { 
      "href": ".../timeline/2025/index.json",
      "title": "Latest year (2025)"
    },
    "latest": { 
      "href": ".../10.0/index.json",
      "title": "Latest .NET release (.NET 10.0)"
    },
    "latest-lts": { 
      "href": ".../8.0/index.json",
      "title": "Latest LTS release (.NET 8.0)"
    }
  }
}
```

**Notes:**
- `latest-year` = primary (qualified to distinguish from version `latest`)
- `latest`, `latest-lts` = cross-references to version hierarchy
- Symmetric with releases-index cross-advertising

---

### 6. year-index (e.g., timeline/2025/index.json)

**Properties (before `_links`):**
```json
{
  "kind": "year-index",
  "title": ".NET Release Timeline Index - 2025",
  "description": "...",
  "year": "2025",
  
  "latest-month": "11",          // Latest month (primary)
  
  "_links": {
    "self": { "href": ".../timeline/2025/index.json" },
    "timeline-index": { "href": ".../timeline/index.json" },
    
    "latest-month": { 
      "href": ".../timeline/2025/11/index.json",
      "title": "Latest month (November 2025)"
    }
  }
}
```

**Notes:**
- `latest-month` = primary (latest month in this year)
- Qualified for parallel structure with `latest-year`
- Cache-safe: year-index controls its own months

---

### 7. month-index (e.g., timeline/2025/11/index.json)

**Properties: None** (this IS a specific month - leaf node)

```json
{
  "kind": "month-index",
  "title": ".NET Release Timeline Index - 2025-11",
  "year": "2025",
  "month": "11",
  "_links": {
    "self": { "href": ".../timeline/2025/11/index.json" },
    "year-index": { "href": ".../timeline/2025/index.json" }
  }
}
```

**Notes:**
- Leaf node - no "latest" applicable
- Navigate up only

---

## Cross-Advertising Strategy

### Bidirectional Root-Level Links

**releases-index ↔️ timeline-index**

```
releases-index
  latest-year → timeline/2025/index.json

timeline-index
  latest → 10.0/index.json
  latest-lts → 8.0/index.json
```

**Benefits:**
- Discovery: Users find alternative navigation
- Convenience: Jump between hierarchies easily
- Symmetric: Both indexes advertise each other

**Guidelines:**
- ✅ DO cross-advertise at root level (releases ↔️ timeline)
- ❌ DON'T cross-advertise deeper (major-version ↛ year)

---

## Property Order in JSON Documents

**Canonical order:**
```json
{
  "$schema": "...",
  "kind": "...",
  "title": "...",
  "description": "...",
  
  // Latest properties HERE (alphabetical within group)
  "latest": "...",
  "latest-lts": "...",
  "latest-month": "...",
  "latest-security": "...",
  "latest-year": "...",
  
  "_links": { ... },
  "lifecycle": { ... },
  "usage": { ... },
  "glossary": { ... },
  "_embedded": { ... },
  "_metadata": { ... }
}
```

**Rationale:**
- Properties before `_links` aids quick scanning
- Alphabetical within group (consistent, predictable)
- Matches REST API conventions

---

## Constants Required

```csharp
// LinkRelations.cs

// Latest link relations - Version hierarchy
/// <summary>
/// Link relation for latest item in current collection
/// Context-dependent: latest major (releases-index), latest patch (major-version-index), etc.
/// </summary>
public const string Latest = "latest";

/// <summary>
/// Link relation for latest LTS (Long-Term Support) version
/// </summary>
public const string LatestLts = "latest-lts";

/// <summary>
/// Link relation for latest SDK index
/// </summary>
public const string LatestSdk = "latest-sdk";

/// <summary>
/// Link relation for latest patch with security fixes
/// </summary>
public const string LatestSecurity = "latest-security";

// Latest link relations - Timeline hierarchy
/// <summary>
/// Link relation for latest year in timeline
/// </summary>
public const string LatestYear = "latest-year";

/// <summary>
/// Link relation for latest month in a year
/// </summary>
public const string LatestMonth = "latest-month";
```

---

## Cache Considerations

### ✅ Safe: Single-Level Latest

```
releases-index (fresh)
  latest → 10.0/index.json
```

No inconsistency - property and link in same document.

### ✅ Safe: Latest-Month in Year-Index

```
year-index (fresh OR stale)
  latest-month → 2025/11/index.json
```

No inconsistency - year-index controls its own months.

### ❌ Unsafe: Multi-Level Chain (Avoided)

```
timeline-index (fresh)
  latest-year → 2025/index.json

year-index (STALE)
  latest-month → 2025/10/index.json (OUTDATED!)
```

We avoid this by keeping latest-month in year-index only!

---

## Implementation Checklist

### Phase 1: Add Missing Constants
- [x] `Latest` (already exists)
- [x] `LatestLts` (already exists)
- [x] `LatestSdk` (already exists)
- [x] `LatestYear` (already exists)
- [ ] `LatestSecurity` (NEW)
- [ ] `LatestMonth` (NEW)

### Phase 2: Update Record Definitions
- [ ] Add latest properties to record types
- [ ] Ensure properties appear before `_links` in serialization
- [ ] Update JSON serialization order

### Phase 3: Implement in VersionIndex
- [ ] releases-index: Add `latest-year` property + link
- [ ] major-version-index: Add `latest`, `latest-security` properties + links
- [ ] sdk-index: Add `latest` property + link

### Phase 4: Implement in ShipIndex
- [ ] timeline-index: Add `latest-year`, `latest`, `latest-lts` properties + links
- [ ] year-index: Add `latest-month` property + link

### Phase 5: Validation
- [ ] Verify property order in generated JSON
- [ ] Test all latest links navigate correctly
- [ ] Validate cross-advertising works
- [ ] Confirm cache behavior is safe

---

## Summary

**Key Decisions:**
1. ✅ **Context-dependent `latest`** - Container provides noun phrase
2. ✅ **Qualified only when needed** - Subsets, cross-refs, disambiguation
3. ✅ **Properties before `_links`** - Quick scanning
4. ✅ **Root-level cross-advertising** - Discovery between hierarchies
5. ✅ **Cache-friendly design** - No multi-level chains

**Coverage:**
- releases-index: `latest`, `latest-lts`, `latest-year` (+ `latest-sdk` link)
- major-version-index: `latest`, `latest-security`
- sdk-index: `latest`
- timeline-index: `latest-year`, `latest`, `latest-lts`
- year-index: `latest-month`

**Benefits:**
- Concise and clean (follows REST conventions)
- Unambiguous in context (document provides noun)
- Discoverable (cross-advertising between hierarchies)
- Cache-safe (single-level latest only)
