# Index Generation Tools

Two complementary tools for generating .NET release information indexes.

## Tools

### VersionIndex
**Location:** `src/VersionIndex/`  
**Purpose:** Version-centric hierarchy (major versions → patch versions)

Generates:
- Root `index.json` with all major versions
- Per-version `{version}/index.json` with patch releases
- SDK indexes for .NET 8.0+
- Lifecycle manifests

**Usage:**
```bash
dotnet run --project src/VersionIndex -- ~/git/core-rich/release-notes
```

### ShipIndex
**Location:** `src/ShipIndex/`  
**Purpose:** Time-centric hierarchy (years → months → ship days)

Generates:
- `archives/index.json` with all years
- Per-year `archives/{year}/index.json`
- Per-month `archives/{year}/{month}/index.json`
- CVE information

**Usage:**
```bash
dotnet run --project src/ShipIndex -- ~/git/core-rich/release-notes
```

## Execution Order

Both tools can run **independently in any order**:
- Each reads from source files (releases.json, release.json, cve.json)
- Neither depends on the other's generated output
- They can run in parallel if desired

**Recommended workflow:**
```bash
# Run both (order doesn't matter)
dotnet run --project src/VersionIndex -- ~/git/core-rich/release-notes
dotnet run --project src/ShipIndex -- ~/git/core-rich/release-notes
```

## Cross-Linking

The tools generate links to each other based on **convention**:
- VersionIndex links patch versions to `archives/{year}/{month}/index.json` (calculated from release date)
- ShipIndex links ship days to `{version}/{patch}/release.json` (existing source files)
- No File.Exists checks - assumes the other side will generate expected files

See `docs/version-ship-cross-linking.md` for detailed cross-linking design.

## Shared Dependencies

Both tools share:
- `DotnetRelease` project - Core data models and types
- `JsonSchemaInjector` - Adds $schema references to generated JSON
- Helper files - HalJsonComparer, HalLinkGenerator, Summary, etc.

## DotnetRelease Structure

The shared `DotnetRelease` project now uses an organized directory structure:

```
DotnetRelease/
├── DataModel/
│   ├── HalJson/          # HAL+JSON index types
│   ├── Cves/             # CVE record types
│   ├── ReleaseInfo/      # Release.json types
│   ├── Legacy/           # Old index formats
│   └── Other/            # OS packages, etc.
├── ReleaseSummary/       # Summary types for generation
├── KebabCaseLowerConverter.cs
├── Location.cs
├── ReleaseNotes.cs
├── SdkBand.cs
└── SupportedOS.cs
```

## Migration from UpdateIndexes

The original `UpdateIndexes` tool in the worktree has been split into:
- **VersionIndex** - ReleaseIndexFiles.cs + SdkIndexFiles.cs
- **ShipIndex** - HistoryIndexFiles.cs

Benefits of splitting:
- Clearer separation of concerns
- Independent execution and testing
- Easier to understand and maintain
- Better naming (ShipIndex vs "History")
