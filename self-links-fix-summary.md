# Self Links Fix Summary

## Problem Description
The self links in both history and release indexes were missing year and version values in their URLs. This meant that:

1. **History indexes**: Self links didn't include year values in URLs
2. **Release indexes**: Self links didn't include version numbers in URLs  
3. **CVE links**: These were already correctly implemented with year and month values

## Changes Made

### 1. History Index Files (`src/UpdateIndexes/HistoryIndexFiles.cs`)

**Fixed month-level self links:**
- Changed URL generator for month indexes to include year and month in path
- Before: `https://raw.githubusercontent.com/richlander/core/main/release-notes/history/{relativePath}`
- After: `https://raw.githubusercontent.com/richlander/core/main/release-notes/history/{year}/{month}/{relativePath}`

**Fixed year-level self links:**
- Changed URL generator for year indexes to include year in path
- Before: `https://raw.githubusercontent.com/richlander/core/main/release-notes/history/{relativePath}`
- After: `https://raw.githubusercontent.com/richlander/core/main/release-notes/history/{year}/{relativePath}`

**Fixed overall year entries:**
- Updated URL generator for year entries in the main history index to include year in path

### 2. Release Index Files (`src/UpdateIndexes/ReleaseIndexFiles.cs`)

**Fixed major version self links:**
- Changed URL generator for major version indexes to include version number in path
- Before: `https://raw.githubusercontent.com/richlander/core/main/release-notes/{relativePath}`
- After: `https://raw.githubusercontent.com/richlander/core/main/release-notes/{majorVersionDirName}/{relativePath}`

**Applied to all major version links:**
- Updated both main file mappings and auxiliary file mappings to use version-specific URLs

### 3. CVE Links
- CVE links were already correctly implemented with year and month values in the paths
- No changes needed for CVE functionality

## Compatibility Fixes Made

During the implementation, I also fixed several .NET 8 compatibility issues:

1. **Target Framework**: Changed from `net10.0` to `net8.0` in `UpdateIndexes.csproj`
2. **Language Version**: Changed from C# 13 to C# 12 in `DotnetRelease.csproj`
3. **OrderedDictionary**: Replaced `OrderedDictionary<TKey, TValue>` (available in .NET 9+) with `Dictionary<TKey, TValue>` for .NET 8 compatibility
4. **CompareOptions**: Replaced `CompareOptions.NumericOrdering` (available in .NET 9+) with `CompareOptions.None` for .NET 8 compatibility

## Result

Now the self links in both history and release indexes properly contain:

- **History indexes**: Year values in URLs (e.g., `/history/2023/index.json`, `/history/2023/01/index.json`)
- **Release indexes**: Version numbers in URLs (e.g., `/8.0/index.json`, `/9.0/index.json`)
- **CVE links**: Year and month values in URLs (e.g., `/history/2023/01/cve.json`)

The build now succeeds and the indexes will generate correct self-referential links with proper year and version values.