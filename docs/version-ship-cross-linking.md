# Cross-Linking Design: VersionIndex and ShipIndex

## Overview

The .NET release information system consists of two complementary graphs:

1. **VersionIndex** (`index.json`, `{version}/index.json`) - Version-centric hierarchy
2. **ShipIndex** (`archives/index.json`, `archives/{year}/index.json`) - Time-centric hierarchy

These graphs should cross-link to enable navigation between different access patterns.

## Key Cross-Linking Opportunities

### 1. Root Level Bidirectional Links ✅
Already implemented - both roots link to each other.

### 2. Patch Version → Ship Day (RECOMMENDED)
From VersionIndex patch entries, link to the ShipIndex month/day showing what else shipped.
- **Benefit**: "What else shipped with .NET 8.0.11?"
- **Implementation**: Add `ship-day` link based on release-date
- **Cache-friendly**: Yes (dates are immutable)

### 3. Ship Day → Patch Versions
From ShipIndex day entries, link back to VersionIndex patch release details.
- **Benefit**: "Show me details for each version shipped on 2024-11-12"
- **Implementation**: ShipIndex adds links to release.json files

### 4. CVE Cross-Linking
- ShipIndex owns authoritative CVE records
- VersionIndex references CVE summaries with links back to ShipIndex
- **Benefit**: Navigate from version → CVEs → other affected versions

### 5. Aggregation Links
- Year/month aggregations link to relevant version indexes
- Version indexes link to relevant ship history periods

## Tool Execution Order & Dependencies

### Analysis of Cross-Link Types

**Type A: Self-Contained Links** (no dependency on other tool's output)
- VersionIndex: Patch → Ship day (calculated from release-date in source data)
- ShipIndex: Root → VersionIndex root (static path)
- VersionIndex: Root → ShipIndex root (static path)

**Type B: Requires Other Tool's Output**
- ShipIndex: Ship day → Patch versions (needs to know which versions exist)
- ShipIndex: Month/Year → Version indexes (needs version list)
- VersionIndex: Patch → CVE records (needs CVE file paths from ShipIndex)

### Single-Pass Solution (RECOMMENDED)

Both tools can run **independently in any order** by reading from **source data only**:

1. **VersionIndex** reads:
   - Release dates from `{version}/{patch}/release.json` files
   - Calculates ship-day links as `archives/{year}/{month}/index.json`
   - No need to wait for ShipIndex to run

2. **ShipIndex** reads:
   - Release dates from same `{version}/{patch}/release.json` files
   - Builds calendar structure
   - Creates links to `{version}/{patch}/release.json` files that already exist
   - No need to wait for VersionIndex to run

**Key insight**: Both tools read the **same source data** (releases.json, release.json files), not each other's generated indexes.

### Execution Options

**Option 1: Independent Execution** (RECOMMENDED)
```bash
# Can run in any order or in parallel
dotnet run --project VersionIndex -- ~/git/core-rich/release-notes
dotnet run --project ShipIndex -- ~/git/core-rich/release-notes
```

**Option 2: Multi-Pass** (only if we want generated → generated links)
```bash
# Pass 1: Generate both indexes without cross-tool links
dotnet run --project VersionIndex -- ~/git/core-rich/release-notes
dotnet run --project ShipIndex -- ~/git/core-rich/release-notes

# Pass 2: Enhance with generated → generated links (optional)
dotnet run --project VersionIndex --enhance -- ~/git/core-rich/release-notes
dotnet run --project ShipIndex --enhance -- ~/git/core-rich/release-notes
```

### Recommended Approach

**Single-pass, independent execution** because:
- All necessary data exists in source files
- Tools don't need to read each other's generated indexes
- Simpler to maintain and reason about
- Can run in parallel
- No ordering constraints

Links should point to **source files** (release.json) and **well-known paths** (archives/{year}/{month}/index.json), not to generated index.json files.
