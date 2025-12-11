# Patch Entry Shape Specification

This document defines the shape for patch release entries as they appear embedded in various index contexts throughout the .NET release metadata graph.

## Vocabulary

The graph uses specific terms consistently. Understanding these is essential for navigating and querying effectively.

### Version Granularity

| Term | Granularity | Examples | Description |
|------|-------------|----------|-------------|
| **release** | Major | `"9.0"`, `"10.0"` | A major .NET version with its own support lifecycle |
| **patch** | Patch | `"9.0.10"`, `"10.0.1"` | A servicing update to a major release |
| **preview** | Patch | `"10.0.0-preview.7"`, `"10.0.0-rc.2"` | Pre-release patch versions |

### Property Naming Conventions

**Top-level properties** (scalars and vectors of scalars):

| Property | Type | Contains | Used For |
|----------|------|----------|----------|
| `release` | scalar | Major version | Filtering, grouping by major version |
| `releases` | vector | Major versions | Listing which releases are relevant in a context |
| `version` | scalar | Full version | Unique identification of a specific patch or release |
| `latest` | scalar | Major version | Quick access to the newest major release |
| `latest_patch` | scalar | Patch version | Quick access to the newest patch |
| `latest_release` | scalar | Major version | Newest major version in a time-scoped context |
| `runtime_patches` | vector | Patch versions | Listing patch versions (in month summaries) |
| `cve_records` | vector | CVE IDs | Listing CVE identifiers |

**`_embedded` properties** (vectors of objects):

| Property | Entry Shape | Description |
|----------|-------------|-------------|
| `releases` | Major version entry | Major release entries (root index only) |
| `patches` | Patch entry | Patch release entries |
| `latest_patches` | Patch entry | Latest patch per supported release |
| `latest_security_month` | Security status entry | Security patch info per release for latest security month |
| `months` | Month summary | Monthly aggregates within a year |
| `years` | Year summary | Yearly aggregates |
| `disclosures` | CVE disclosure | Security vulnerability details |
| `sdk_feature_bands` | SDK band entry | SDK versions by feature band |

**Design note:** The same name (e.g., `releases`) can appear as both a scalar array at top level and an object array in `_embedded`. This is intentional:

- Top-level `releases: ["10.0", "9.0", "8.0"]` — quick filtering, lightweight
- `_embedded.releases: [{...}, {...}]` — full entry objects with links and metadata

The top-level scalar array acts as a "table of contents" for what's in the embedded collection. This enables queries like:

```bash
# Check if 9.0 is present (fast, no iteration)
jq '.releases | index("9.0")'

# Get full 9.0 entry (in root index)
jq '._embedded.releases[] | select(.version == "9.0")'

# Get 9.0 patches (in month index)
jq '._embedded.patches[] | select(.release == "9.0")'
```

### The `release` vs `version` Distinction

This is the critical vocabulary decision:

- **`version`** — The precise identifier. For patches: `"9.0.10"`. For major versions: `"9.0"`.
- **`release`** — The major version a patch belongs to. Always a major version like `"9.0"`.

In a patch entry:
```json
{
  "version": "9.0.10",   // This specific patch
  "release": "9.0"       // The major release it belongs to
}
```

In a major version entry:
```json
{
  "version": "9.0"       // This major release (no `release` property needed)
}
```

### Plural Forms

| Singular | Plural | Contains |
|----------|--------|----------|
| `release` | `releases` | Major version strings (or major version entries in `_embedded`) |
| `patch` | `patches` | Patch entries (in `_embedded`) |
| `cve_record` | `cve_records` | CVE identifiers |
| `runtime_patch` | `runtime_patches` | Patch version strings |
| `latest_patch` | `latest_patches` | Patch entries (in `_embedded`) |

### Embedded Collections by Context

Different index types embed different collections:

| Parent `kind` | Embedded collection | Entry shape |
|---------------|---------------------|-------------|
| `releases-index` | `_embedded.releases` | Major version entry |
| `major-version-index` | `_embedded.patches` | Patch entry |
| `month-index` | `_embedded.patches` | Patch entry |
| `year-index` | `_embedded.months`, `_embedded.releases` | Month summary, Major version entry |
| `timeline-index` | `_embedded.years` | Year summary |
| `llms-index` | `_embedded.latest_patches` | Patch entry |
| `patch-version-index` | `_embedded.sdk_feature_bands`, `_embedded.disclosures` | SDK band, CVE disclosure |

This vocabulary distinction makes the schema self-documenting:
- `_embedded.releases` always contains major version entries
- `_embedded.patches` always contains patch entries
- No need to check parent `kind` to know what shape you're getting

## Design Goal

Reuse the same type shape across all contexts where patch entries appear. This is simpler and arguably more correct—the "burden" on that shape varies by context, but the shape itself remains consistent.

## The Patch Entry Shape

A patch entry represents a single runtime patch release (e.g., `10.0.1`, `9.0.10`, `8.0.21`).

```json
{
  "version": "9.0.10",
  "release": "9.0",
  "date": "2025-10-14T00:00:00+00:00",
  "year": "2025",
  "month": "10",
  "security": true,
  "cve_count": 3,
  "cve_records": ["CVE-2025-55247", "CVE-2025-55248", "CVE-2025-55315"],
  "support_phase": "active",
  "sdk_version": "9.0.306",
  "_links": {
    "self": {
      "href": ".../9.0/9.0.10/index.json"
    }
  }
}
```

**Note on `_links`:** The patch entry shape requires only `self`. Additional links (e.g., `release-month`, `latest-sdk`) may be present depending on context but are not part of the core shape.

### Core Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `version` | string | Yes | Full patch version (e.g., `"9.0.10"`, `"10.0.0-rc.2"`) |
| `release` | string | Yes | Major version this patch belongs to (e.g., `"9.0"`, `"10.0"`) |
| `date` | ISO 8601 | No | Release date (omitted for preview releases without fixed dates) |
| `year` | string | No | Release year (e.g., `"2025"`) |
| `month` | string | No | Release month (e.g., `"10"`) |
| `security` | boolean | Yes | Whether this release includes security fixes |
| `cve_count` | integer | Yes | Number of CVEs addressed (0 if not a security release) |
| `cve_records` | array | No | CVE identifiers (omitted when `cve_count` is 0) |
| `support_phase` | enum | Yes | Current phase: `preview`, `go-live`, `active`, `maintenance`, `eol` |
| `sdk_version` | string | No | SDK patch version shipped with this runtime patch |
| `release_type` | enum | No | Release type: `lts` or `sts` (useful when filtering without context) |

### The `release` Property

**This is the key addition for currency consistency.**

The `release` property provides the major version identifier, enabling queries like:

```bash
# Filter embedded patches by major version
jq '._embedded.releases[] | select(.release == "10.0")'
```

This works regardless of whether the context's `.releases` array uses major versions (like in month indexes) or the embedded entries use full patch versions.

## Contexts Where Patch Entries Appear

### 1. Month Index (`timeline/2025/10/index.json`)

**Context properties:**
- `.releases` = `["10.0", "9.0", "8.0"]` (major versions active this month)
- `.security`, `.cve_count`, `.cve_records` (aggregated for the month)

**Embedded:** `._embedded.patches[]` — all patches released this month

**Common queries:**

| Query | jq Expression | Works? |
|-------|---------------|--------|
| All patches this month | `._embedded.patches[]` | ✓ |
| Patches for .NET 9.0 | `._embedded.patches[] \| select(.release == "9.0")` | ✓ |
| Security patches only | `._embedded.patches[] \| select(.security)` | ✓ |
| Get to 9.0 patch details | `._embedded.patches[] \| select(.release == "9.0") \| ._links.self.href` | ✓ |

**Currency analysis:**
- `.releases` uses major versions → `.release` property on patch entries enables filtering
- The name `patches` makes it clear these are patch entries, not major version entries

**Duplication with context:**
- `year`, `month` duplicate the context's `.year`, `.month` — acceptable for self-contained entries
- `cve_records` may partially duplicate context's aggregate — provides per-patch granularity

### 2. Major Version Index (`10.0/index.json`)

**Context properties:**
- Implicit: this is the 10.0 index, so all patches are for 10.0
- `.latest`, `.latest_security` (patch versions)
- `.release_type`, `.support_phase`, `.supported`, `.ga_date`, `.eol_date`

**Embedded:** `._embedded.patches[]` — all patches for this major version

**Common queries:**

| Query | jq Expression | Works? |
|-------|---------------|--------|
| All patches | `._embedded.patches[]` | ✓ |
| Latest patch | `._embedded.patches[0]` (ordered by date desc) | ✓ |
| Security patches | `._embedded.patches[] \| select(.security)` | ✓ |
| Patches from October 2025 | `._embedded.patches[] \| select(.year == "2025" and .month == "10")` | ✓ |
| GA releases only | `._embedded.patches[] \| select(.support_phase == "active")` | ✓ |

**Currency analysis:**
- `.release` would always be `"10.0"` — technically redundant but consistent
- Having it enables copy-paste of entries between contexts without modification

**Duplication with context:**
- `.release` duplicates implicit context — low cost, high consistency value

### 3. Year Index (`timeline/2025/index.json`)

**Context properties:**
- `.year` = `"2025"`
- `.releases` = `["10.0", "9.0", "8.0"]` (major versions with activity this year)

**Embedded:** `._embedded.releases[]` — major version summaries (not patch entries)

**Note:** Year indexes embed major version summaries, not patch entries. The `._embedded.months[]` array provides month-level summaries with `runtime_patches` arrays.

This is a different shape and appropriately so—year indexes aggregate at a higher level.

### 4. LLMs Index (`llms.json`) — Proposed

**Context properties:**
- `.latest_patch`, `.latest_lts_patch` (specific patch versions)
- `.supported_releases` = `["10.0", "9.0", "8.0"]` (major versions)

**Proposed embedded:** `._embedded.latest_patches[]` — latest patch for each supported release

**Common queries:**

| Query | jq Expression | Works? |
|-------|---------------|--------|
| All latest patches | `._embedded.latest_patches[]` | ✓ |
| Latest for .NET 9.0 | `._embedded.latest_patches[] \| select(.release == "9.0")` | ✓ |
| Any critical security? | `._embedded.latest_patches[] \| select(.security)` | ✓ |
| Jump to 9.0 patch index | `._embedded.latest_patches[] \| select(.release == "9.0") \| ._links.self.href` | ✓ |

**Currency analysis:**
- `.supported_releases` uses major versions → `.release` property enables filtering
- Name change from `supported_releases` to `latest_patches` clarifies the embedded content

**Duplication with context:**
- Minimal — context has version strings, embedded has full entries

### 5. Root Releases Index (`index.json`)

**Embedded:** `._embedded.releases[]` — major version entries (not patch entries)

This is a different shape (major version entries with `release_type`, `supported`, etc.) and appropriately so.

## Shape Comparison: Patch Entry vs Month Entry

For completeness, here's how month entries (in year indexes) differ:

**Month entry shape** (in `._embedded.months[]`):

```json
{
  "month": "10",
  "security": true,
  "cve_count": 3,
  "cve_records": ["CVE-..."],
  "latest_release": "9.0",
  "releases": ["10.0", "9.0", "8.0"],
  "runtime_patches": ["10.0.0-rc.2", "9.0.10", "8.0.21"],
  "_links": { "self": {...}, "cve-json": {...} }
}
```

Month entries are aggregates—they summarize what happened in a month. They don't need a `release` property because they span multiple major versions by design.

## Recommendations

1. **Add `release` property to patch entries** — enables consistent filtering across all contexts

2. **Rename `_embedded.supported_releases` to `_embedded.latest_patches` in llms.json** — clarifies that these are patch entries, not major version entries

3. **Keep the unified shape** — the minor redundancy (e.g., `release: "10.0"` in the 10.0 index) is worth the consistency benefit

4. **Document the currency convention** — major version filtering uses `.release`, patch version identification uses `.version`

## Example: Updated llms.json

```json
{
  "kind": "llms-index",
  "title": ".NET Release Index for AI",
  "ai_note": "Before navigating this graph, read the guide at llms-txt; it explains optimal query patterns.",
  "latest": "10.0",
  "latest_lts": "10.0",
  "latest_patch": "10.0.1",
  "latest_lts_patch": "10.0.1",
  "supported_releases": ["10.0", "9.0", "8.0"],
  "_links": { ... },
  "_embedded": {
    "latest_patches": [
      {
        "version": "10.0.1",
        "release": "10.0",
        "date": "2025-12-09T00:00:00+00:00",
        "year": "2025",
        "month": "12",
        "security": false,
        "cve_count": 0,
        "support_phase": "active",
        "sdk_version": "10.0.101",
        "_links": {
          "self": { "href": ".../10.0/10.0.1/index.json" }
        }
      },
      {
        "version": "9.0.11",
        "release": "9.0",
        "date": "2025-11-19T00:00:00+00:00",
        "year": "2025",
        "month": "11",
        "security": false,
        "cve_count": 0,
        "support_phase": "active",
        "sdk_version": "9.0.308",
        "_links": {
          "self": { "href": ".../9.0/9.0.11/index.json" }
        }
      },
      {
        "version": "8.0.22",
        "release": "8.0",
        "date": "2025-11-11T00:00:00+00:00",
        "year": "2025",
        "month": "11",
        "security": false,
        "cve_count": 0,
        "support_phase": "active",
        "sdk_version": "8.0.416",
        "_links": {
          "self": { "href": ".../8.0/8.0.22/index.json" }
        }
      }
    ]
  }
}
```

Now `._embedded.latest_patches[] | select(.release == "9.0")` works, and the currency matches `.supported_releases`.

## Security Status Entry Shape

In addition to patch entries, the LLMs index includes a `latest_security_month` collection that provides a security-focused projection per release.

This is not a full patch entry—it's a "security status entry" that answers: "What do I need to know about security for each release?"

```json
{
  "year": "2025",
  "month": "10",
  "release": "9.0",
  "release_type": "sts",
  "version": "9.0.10",
  "sdk_version": "9.0.306",
  "security": true,
  "cve_count": 3,
  "cve_records": ["CVE-2025-55247", "CVE-2025-55248", "CVE-2025-55315"],
  "_links": {
    "self": {
      "href": ".../timeline/2025/10/index.json"
    }
  }
}
```

**Key differences from patch entry:**

1. **Temporal properties first** — `year`, `month` come before `release` to emphasize "in this month, for this release..."
2. **`_links.self` points to the month index** — not the patch index. The month is the navigational target for full CVE details.
3. **Purpose is comparison** — "Is my version (`version`) at or ahead of the latest security patch?"

**Common queries:**

```bash
# Latest security patch for 9.0
jq '._embedded.latest_security_month[] | select(.release == "9.0") | .version'

# Am I behind on security? (compare against your version)
jq '._embedded.latest_security_month[] | select(.release == "9.0" and .version != "9.0.10")'

# Get CVE details for a release
jq '._embedded.latest_security_month[] | select(.release == "9.0") | ._links.self.href'
```

## Known Issues

### Missing `latest-security-month` link in major version indexes

The `10.0/index.json` has `latest_security: "10.0.0-rc.2"` and a `latest-security` link, but is missing the `latest-security-month` link to `timeline/2025/10/index.json`.

Additionally, the rc.2 patch shows `security: true` with `cve_count: 2`, but the actual CVE disclosures in `timeline/2025/10/index.json` do not list 10.0 in their `affected_releases`. This appears to be a data inconsistency.

**Impact:** The `latest_security_month` collection in llms.json should only include releases that were actually affected by CVEs in that month. If 10.0 wasn't affected, it should not appear.

**Resolution:** Verify CVE data and ensure `security` and `cve_count` on patch entries accurately reflect the CVEs that list that release in `affected_releases`.
