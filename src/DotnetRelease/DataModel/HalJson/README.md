# HAL+JSON Object Models Documentation

This directory contains object models for [HAL+JSON](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal) hypermedia documents that provide structured access to .NET release information. These models enable machine-readable navigation through the .NET release ecosystem while maintaining human-friendly representations.

## Overview

The HAL+JSON object models follow the [HAL specification](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal) and provide a hypermedia API for .NET release data. They enable tools and applications to programmatically discover and navigate .NET release information through standardized link relationships and embedded resources.

### Core Principles

- **Hypermedia-driven**: Navigation through `_links` and `_embedded` properties
- **Self-describing**: Each document includes `kind` and `description` properties
- **Consistent linking**: Standardized link object structure with `href`, `relative`, `title`, and `type`
- **Hierarchical organization**: Supports both version-based and chronological navigation
- **Rich metadata**: Includes support lifecycle, CVE information, and component details

## Object Models

### ReleaseVersionIndex

**Purpose**: Provides an index of .NET releases organized by version hierarchy (major → patch releases).

**Schema**: `release-version-index.json`

**Key Properties**:
- `kind`: Always `"index"` for version-based indexes
- `description`: Human-readable description of the index scope
- `support`: Optional support lifecycle information (GA date, EOL date, release type, phase)
- `_embedded.releases`: List of version entries with navigation links

**Usage Patterns**:
- Root index (`/index.json`): Lists all major .NET versions
- Major version index (`/8.0/index.json`): Lists all patch releases for .NET 8.0

**Example**:
```json
{
  "$schema": "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/release-version-index.json",
  "kind": "index",
  "description": "Index of .NET major versions",
  "_links": {
    "self": {
      "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/index.json",
      "relative": "index.json",
      "title": ".NET Release",
      "type": "application/hal+json"
    }
  },
  "_embedded": {
    "releases": [
      {
        "version": "10.0",
        "kind": "index",
        "_links": { ... },
        "support": {
          "release-type": "sts",
          "phase": "preview",
          "ga-date": "2025-11-11T00:00:00+00:00",
          "eol-date": "2028-11-14T00:00:00+00:00"
        }
      }
    ]
  }
}
```

### ReleaseHistoryIndex

**Purpose**: Provides chronological access to .NET releases organized by time periods (years → months → releases).

**Schema**: `release-history-index.json`

**Key Properties**:
- `kind`: Type of history index (`release-history-index`, `history-year-index`, `history-month-index`)
- `description`: Context-aware description of the time period
- `year`: Year identifier for year and month indexes
- `month`: Month identifier for month indexes
- `_embedded.years`: Yearly navigation entries (root level)
- `_embedded.months`: Monthly navigation entries (year level)
- `_embedded.releases`: Release version summaries

**Hierarchy**:
1. **Root History Index** (`/archives/index.json`): Lists years with .NET releases
2. **Year Index** (`/archives/2025/index.json`): Lists months and releases for 2025
3. **Month Index** (`/archives/2025/02/index.json`): Detailed releases for February 2025

**Example**:
```json
{
  "$schema": "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/release-history-index.json",
  "kind": "release-history-index",
  "description": "History of .NET releases",
  "_embedded": {
    "years": [
      {
        "kind": "history-year-index",
        "description": ".NET release history for 2025",
        "year": "2025",
        "_links": { ... },
        "dotnet-releases": ["8.0", "9.0", "10.0"]
      }
    ],
    "releases": [
      {
        "version": "10.0",
        "_links": { ... }
      }
    ]
  }
}
```

### ReleaseManifest

**Purpose**: Contains comprehensive metadata about a specific .NET major release, including support lifecycle information.

**Schema**: `release-manifest.json`

**Key Properties**:
- `kind`: Always `"manifest"`
- `version`: Major version identifier (e.g., "8.0")
- `label`: Human-friendly version label
- `ga-date`: General Availability release date
- `eol-date`: End of Life date
- `release-type`: Release support model (`"lts"` or `"sts"`)
- `support-phase`: Current lifecycle phase (`"preview"`, `"active"`, `"maintenance"`, `"eol"`)

**Example**:
```json
{
  "$schema": "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/release-manifest.json",
  "kind": "manifest",
  "version": "8.0",
  "label": ".NET 8.0",
  "ga-date": "2023-11-14T00:00:00+00:00",
  "eol-date": "2026-11-10T00:00:00+00:00",
  "release-type": "lts",
  "support-phase": "active",
  "_links": { ... }
}
```

## Common Types and Enumerations

### ReleaseKind Enumeration

Identifies the type of release or index document:

- `"index"`: Version-based index document
- `"manifest"`: Release metadata document
- `"major-release"`: Major version content
- `"patch-release"`: Patch version content
- `"content"`: General content document
- `"unknown"`: Unspecified type

### HistoryKind Enumeration

Identifies the type of history index document:

- `"release-history-index"`: Root chronological index
- `"history-year-index"`: Year-specific index
- `"history-month-index"`: Month-specific index

### Support Information

Provides lifecycle metadata for .NET releases:

```json
{
  "release-type": "lts|sts",
  "phase": "preview|active|maintenance|eol",
  "ga-date": "ISO 8601 datetime",
  "eol-date": "ISO 8601 datetime"
}
```

**Release Types**:
- `"lts"`: Long-Term Support (3 years)
- `"sts"`: Standard-Term Support (18 months)

**Support Phases**:
- `"preview"`: Pre-release development
- `"active"`: Currently supported with regular updates
- `"maintenance"`: Security and critical fixes only
- `"eol"`: End of Life, no longer supported

### Link Objects

All HAL+JSON documents use standardized link objects:

```json
{
  "href": "https://full.url/to/resource",
  "relative": "relative/path/to/resource",
  "title": "Human-readable link title",
  "type": "application/hal+json"
}
```

**Properties**:
- `href`: Absolute URL to the linked resource
- `relative`: Relative path from the document root
- `title`: Human-readable description
- `type`: Media type (typically `"application/hal+json"` or `"application/json"`)

## Navigation Patterns

### Version-First Navigation

For applications that need to explore releases by version:

1. Start with root index (`/index.json`)
2. Navigate to major version (`/8.0/index.json`)
3. Access specific patch release (`/8.0/8.0.1/release.json`)

### Time-First Navigation

For applications that need chronological access:

1. Start with history index (`/archives/index.json`)
2. Navigate to specific year (`/archives/2025/index.json`)
3. Access monthly details (`/archives/2025/02/index.json`)

### Cross-References

The two navigation models cross-reference each other:
- Version indexes can link to historical timeline entries
- History indexes can link to version-specific content
- Both can reference CVE information and manifests

## Schema Integration

All generated JSON documents automatically include `$schema` references:

```json
{
  "$schema": "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/[schema-name].json",
  ...
}
```

This enables:
- Automatic validation in JSON editors
- IntelliSense and autocomplete support
- Documentation integration in development tools
- Consistent structure verification

## Implementation Notes

### AOT Compatibility

All object models are designed for Native AOT compatibility:
- Uses source-generated JSON serialization contexts
- Avoids reflection-based serialization
- Optimized for minimal runtime dependencies

### Serialization

Object models use `System.Text.Json` with:
- Kebab-case property naming (`PropertyNamingPolicy.KebabCaseLower`)
- Null value omission for optional properties
- Custom enum converters for kebab-case serialization

### Extensibility

The HAL+JSON format supports extensibility:
- Additional properties can be added without breaking compatibility
- Link relations can be extended with new relationship types
- Embedded resources support multiple representation flavors

## Related Documentation

- [HAL Specification](https://datatracker.ietf.org/doc/html/draft-kelly-json-hal)
- [.NET Release Terminology](https://raw.githubusercontent.com/richlander/core/main/release-notes/terminology.md)
- [UpdateIndexes Tool Documentation](../../UpdateIndexes/README.md)