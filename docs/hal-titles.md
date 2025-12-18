# HAL Title Specification

Titles follow the pattern `{Name} - {Value}` where Value is typically a version or date.

## Title Patterns

Most link titles describe the **resource type** being linked to (e.g., ".NET Major Release Index - 10.0"). However, `latest*` and `prev*` links use **navigation relationship** titles instead (e.g., "Latest patch - 10.0.1", "Previous patch release - 10.0.0"). This emphasizes the contrast between paired relations:

- `latest` / `prev` - navigating between patch releases
- `latest-security` / `prev-security` - navigating between security releases
- `latest-year` / `prev` (year) - navigating between years
- `latest-month` / `prev` (month) - navigating between months

### Context-Aware Titles

Links in **index files** use full titles with version/date suffixes (e.g., ".NET Major Release Index - 10.0") because they cross context boundaries. Links in **manifest files** use simple titles (e.g., "Release notes", "Compatibility") because the version context is already established by the parent document. This reduces token usage for LLM consumers while maintaining clarity where context changes.

## Graph Structure

The release index is organized into two parallel hierarchies:

### Version Index

Organizes releases by .NET version number.

| Level | Kind | Path | Description |
|-------|------|------|-------------|
| Root | `releases-index` | `release-notes/index.json` | All .NET releases |
| Major | `major-version-index` | `release-notes/{version}/index.json` | A major release (e.g., 10.0) |
| Patch | `patch-version-index` | `release-notes/{version}/{patch}/index.json` | A patch release (e.g., 10.0.1) |

### Timeline Index

Organizes releases by date (year/month).

| Level | Kind | Path | Description |
|-------|------|------|-------------|
| Root | `timeline-index` | `release-notes/timeline/index.json` | All releases by date |
| Year | `year-index` | `release-notes/timeline/{year}/index.json` | Releases in a year |
| Month | `month-index` | `release-notes/timeline/{year}/{month}/index.json` | Releases in a month |

Source code: `src/VersionIndex/` (version), `src/ShipIndex/` (timeline), `src/LlmsIndex/` (llms.json)

## Titles

| Kind | Relation | Name | Example |
|------|----------|------|---------|
| root | | .NET Release Index | .NET Release Index |
| root | | .NET Major Release Index | .NET Major Release Index - 10.0 |
| root | | .NET Patch Release Index | .NET Patch Release Index - 10.0.1 |
| root | | .NET Release Timeline Index | .NET Release Timeline Index |
| root | | .NET Year Timeline Index | .NET Year Timeline Index - 2025 |
| root | | .NET Month Timeline Index | .NET Month Timeline Index - December 2025 |
| link | `latest` | Latest release | Latest release - .NET 10.0 |
| link | `latest-lts` | Latest LTS release | Latest LTS release - .NET 10.0 |
| link | `latest-security` | Latest security patch | Latest security patch - .NET 10.0.0-rc.2 |
| link | `latest-security-month` | Latest security month | Latest security month - October 2025 |
| link | `latest-year` | Latest year | Latest year - 2025 |
| link | `release-major` | Major release | Major release - .NET 10.0 |
| link | `release-manifest` | Manifest | Manifest - .NET 10.0 |
| link | `downloads` | Downloads | Downloads - .NET 10.0 |
| link | `latest-sdk` | Latest SDK | Latest SDK - .NET 10.0 |
| link | `cve-json` | CVE records | CVE records - October 2025 |
| link | `cve-markdown` | CVE records | CVE records - October 2025 |
| link | `cve-markdown-rendered` | CVE records (Rendered) | CVE records (Rendered) - October 2025 |
| link | `timeline-index` | .NET Release Timeline Index | .NET Release Timeline Index |
| link | `year-index` | .NET Year Timeline Index | .NET Year Timeline Index - 2025 |
| link | `release-month` | .NET Month Timeline Index | .NET Month Timeline Index - December 2025 |
| link | `prev` (year) | Previous year | Previous year - 2024 |
| link | `prev` (month) | Previous month | Previous month - November 2025 |
| link | `prev` (patch) | Previous patch release | Previous patch release - 10.0.0 |
| link | `prev-security` (month) | Previous security month | Previous security month - October 2025 |
| link | `prev-security` (patch) | Previous security patch release | Previous security patch release - 10.0.0-rc.2 |

## Special cases

The `self` link in `latest_patches` objects within `llms.json` includes a title that varies based on whether the patch is a security release. When the patch contains CVEs, the title is "Latest security patch - .NET {version}". Otherwise, it is "Latest patch - .NET {version}". This provides symmetry with the `latest-security` link and ensures the titles match when the latest patch is itself a security patch.

The `release-manifest` link title varies by context. In release indexes (version-based), it uses "Manifest - .NET {version}". In timeline indexes (date-based), it uses "Manifest - {month year}" (e.g., "Manifest - December 2025").
