# Latest Link Relations Design

## Overview

The "latest" link relations provide convenient shortcuts to the most recent content in both version-based and timeline-based indexes.

## Design Principles

### 1. Semantic Consistency
Link relation names must have consistent meaning across all documents. The same relation should always point to the same type of resource.

### 2. Cache-Friendly
Links must work well with CDN TTL/caching. Avoid creating parent→child "latest" chains that could result in cached inconsistency.

## Link Relations Defined

### Version-Based (releases-index)

| Relation | Points To | Example | Notes |
|----------|-----------|---------|-------|
| `latest` | Latest major version | `10.0/index.json` | Unambiguous in version context |
| `latest-lts` | Latest LTS major version | `8.0/index.json` | LTS = Long-Term Support (even versions) |
| `latest-sdk` | Latest SDK index | `10.0/sdk/index.json` | Only for versions 8.0+ |

### Timeline-Based (timeline-index)

| Relation | Points To | Example | Notes |
|----------|-----------|---------|-------|
| `latest-year` | Most recent year | `timeline/2025/index.json` | Qualified to avoid ambiguity |

### No latest-month in year-index

**Why not add `latest-month` to year-index?**

Due to CDN caching, it's possible to get:
- Fresh `timeline-index` (with updated `latest-year`)
- Stale `year-index` (with outdated `latest-month`)

This creates an inconsistent cache state for ~5 minutes during TTL refresh.

**Instead:** Clients should read the first entry in `_embedded.months` array, which is already ordered latest-first.

## Naming Rationale

### Why `latest-year` not just `latest`?

**Problem with using `latest` in both contexts:**

```json
// releases-index
{ "latest": "10.0/index.json" }  // means "latest VERSION"

// timeline-index  
{ "latest": "timeline/2025/index.json" }  // means "latest YEAR" ???
```

Same link relation name = different semantics = violation of HAL+JSON principles ❌

**Solution: Qualify temporal "latest":**

```json
// releases-index
{ "latest": "10.0/index.json" }  // latest version (unambiguous)

// timeline-index
{ "latest-year": "timeline/2025/index.json" }  // latest year (explicit)
```

This keeps `latest` as an established convention in REST APIs (meaning "latest version"), while making temporal navigation explicit.

## Implementation

### Constants (LinkRelations.cs)

```csharp
// Version-based latest links
public const string Latest = "latest";           // Latest version
public const string LatestLts = "latest-lts";   // Latest LTS version  
public const string LatestSdk = "latest-sdk";   // Latest SDK

// Timeline-based latest links
public const string LatestYear = "latest-year"; // Latest year
// Note: No LatestMonth to avoid CDN cache inconsistency
```

### Usage Example

```json
// releases-index (index.json)
{
  "kind": "releases-index",
  "latest": "10.0",
  "latest-lts": "8.0",
  "_links": {
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
    }
  }
}

// timeline-index (timeline/index.json)
{
  "kind": "timeline-index",
  "_links": {
    "latest-year": { 
      "href": ".../timeline/2025/index.json",
      "title": "Latest year (2025)"
    }
  },
  "_embedded": {
    "years": [
      { "year": "2025", ... }  // Latest first
    ]
  }
}

// year-index (timeline/2025/index.json)
{
  "kind": "year-index",
  "_links": {
    // No latest-month due to CDN caching concerns
  },
  "_embedded": {
    "months": [
      { "month": "11", ... }  // Latest first - clients read first entry
    ]
  }
}
```

## Benefits

1. **Unambiguous**: Each link relation has one clear meaning
2. **Cache-friendly**: No parent→child latest chains that cause inconsistency
3. **Discoverable**: Qualified names (`latest-year`) are self-documenting
4. **Consistent with REST**: `latest` = latest version is established convention
5. **Simple for clients**: Direct navigation to most recent content

## Related Decisions

- Indexes ordered latest-first in embedded arrays
- Clients read first array entry when no "latest" link available
- CDN cache consistency prioritized over convenience links
