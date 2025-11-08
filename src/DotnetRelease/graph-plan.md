# .NET Release Notes Graph API Design Specification

## Overview

The .NET Release Notes Graph API provides programmatic access to .NET release information through a three-layer architecture built on HAL+JSON hypermedia. The design separates concerns into deserialization (Layer 1), graph navigation (Layer 2), and domain-specific workflows (Layer 3), with intelligent caching at the graph layer.

## Core Design Philosophy

- **Thin library**: Leverage HAL+JSON structure instead of building complex abstractions
- **Progressive disclosure**: Simple tasks are simple; complex tasks are possible
- **Schema-driven**: All HAL documents have corresponding strongly-typed C# OMs
- **Cache-friendly**: The release graph is designed for caching; exploit this
- **Intentionally leaky**: Expose raw documents alongside helpers for flexibility

## Three-Layer Architecture

### Layer 1: Low-Level Deserialization (Stateless, No Cache)

**Purpose**: Pure serialization/deserialization paired with schema OMs.

**Implementation**: Static methods on document classes (e.g., `ReleaseIndexDocument`, `ReleaseVersionDocument`).

```csharp
public class ReleaseIndexDocument
{
    [JsonPropertyName("_links")]
    public Dictionary<string, HalLink>? Links { get; set; }
    
    [JsonPropertyName("_embedded")]
    public EmbeddedReleases? Embedded { get; set; }
    
    // Low-level deserialization
    public static ReleaseIndexDocument Load(Stream stream);
    public static async Task<ReleaseIndexDocument> LoadAsync(Stream stream);
}
```

**Characteristics**:

- No dependencies except System.Text.Json
- No HTTP, no caching, no state
- User controls all I/O and lifetime
- Perfect for testing, custom scenarios, non-HTTP sources

**Usage Example**:

```csharp
using var stream = await client.GetStreamAsync(url);
var doc = await ReleaseIndexDocument.LoadAsync(stream);
var versions = doc.Embedded?.Releases;
```

### Layer 2: Mid-Level Graph APIs (Stateful, Cached)

**Purpose**: Type-safe HAL navigation with automatic caching.

**Implementation**: `ReleaseGraph` class with link-following methods that return document types.

```csharp
public class ReleaseGraph
{
    // Factory
    public static async Task<ReleaseGraph> LoadFromUrlAsync(string url);
    
    // Generic link following
    public async Task<T> FollowLinkAsync<T>(string rel) where T : class;
    
    // Strongly-typed convenience methods
    public async Task<ReleaseVersionDocument> GetLatestLtsDocumentAsync();
    public async Task<SdkIndexDocument> GetLatestSdkDocumentAsync();
    public async Task<ReleaseVersionDocument> GetVersionDocumentAsync(string version);
}
```

**Characteristics**:

- Manages HTTP and caching transparently
- Returns raw document types (OMs)
- Type-safe link following
- Shared cache across all operations
- Good for users who need documents but want navigation help

**Usage Example**:

```csharp
var graph = await ReleaseGraph.LoadFromUrlAsync(rootUrl);
var ltsDoc = await graph.GetLatestLtsDocumentAsync(); // Cached
var patches = ltsDoc.Embedded?.Releases;
```

### Layer 3: High-Level Workflow APIs (Stateful, Cached, Domain-Specific)

**Purpose**: Domain-specific operations that make common tasks trivial.

**Implementation**: Methods on `ReleaseGraph` and helper types (`ReleaseVersion`, `SdkIndex`, `PatchRelease`) that expose high-value workflows.

```csharp
public class ReleaseGraph
{
    // Domain-specific queries
    public IEnumerable<ReleaseVersion> GetVersions();
    public IEnumerable<ReleaseVersion> GetSupportedVersions();
    public ReleaseVersion? GetVersion(string version);
    
    // High-level navigation returning helper types
    public async Task<ReleaseVersion> GetLatestLtsAsync();
    public async Task<SdkIndex> GetLatestSdkAsync();
}

public class ReleaseVersion
{
    // Convenient properties
    public string Version { get; }
    public bool IsSupported { get; }
    public DateOnly? EolDate { get; }
    
    // Navigation
    public async Task<IEnumerable<PatchRelease>> GetPatchesAsync();
    public async Task<SdkIndex> GetSdkIndexAsync();
    
    // Expose underlying document for advanced scenarios
    public ReleaseVersionSummary Document { get; }
}
```

**Characteristics**:

- Maximum convenience for common operations
- Returns helper types with natural C# idioms
- Shares cache with Layer 2
- Domain knowledge embedded (e.g., what "supported" means)

**Usage Example**:

```csharp
var graph = await ReleaseGraph.LoadFromUrlAsync(rootUrl);
var supported = graph.GetSupportedVersions(); // Sync, from embedded data
var lts = await graph.GetLatestLtsAsync(); // Async, fetches if needed
var patches = await lts.GetPatchesAsync(); // Cached appropriately
```

## Caching Strategy

**Cache Scope**: Per `ReleaseGraph` instance via `ILinkFollower` abstraction.

**Implementation**:

```csharp
public interface ILinkFollower
{
    Task<T> FetchAsync<T>(string href) where T : class;
}

public class CachingLinkFollower : ILinkFollower
{
    private readonly HttpClient _client;
    private readonly ConcurrentDictionary<string, object> _cache;
    
    public async Task<T> FetchAsync<T>(string href);
    public void Clear();
    public bool TryEvict(string href);
}
```

**Cache Behavior**:

- Layer 1: No caching (user manages everything)
- Layer 2: Automatic caching by URL
- Layer 3: Shares Layer 2's cache

**Rationale**: Release data changes infrequently (monthly), files are small (15-50KB), and the graph is explicitly designed to be "cache-friendly."

## High-Level Workflow APIs (Phased)

### Phase 1: High-Confidence APIs (Immediate Implementation)

These replicate and improve upon the value provided by the legacy `releases-index.json`:

```csharp
// Support status queries
IEnumerable<ReleaseVersion> GetSupportedVersions();
IEnumerable<ReleaseVersion> GetVersionsByPhase(SupportPhase phase); // active, eol, preview
IEnumerable<ReleaseVersion> GetVersionsByType(ReleaseType type); // lts, sts
bool IsSupported(string version, DateOnly? asOf = null);
DateOnly? GetEolDate(string version);

// Latest version resolution
ReleaseVersion GetLatestRelease(ReleaseType? type = null);
ReleaseVersion GetLatestSupported(ReleaseType? type = null);

// Patch version queries (NEW capability - patches in one place)
IEnumerable<PatchRelease> GetPatches(string majorMinor); // "9.0"
PatchRelease GetLatestPatch(string majorMinor);

// Basic SDK queries
string GetRuntimeForSdk(string sdkVersion); // "9.0.205" -> "9.0"
```

### Phase 2: Medium-Confidence APIs (Near-Term Implementation)

These leverage the new graph structure to solve genuinely hard problems:

```csharp
// SDK feature band support (KILLER FEATURE - hard to reason about today)
IEnumerable<SdkFeatureBand> GetSdkBands(string runtimeVersion);
IEnumerable<string> GetSdkVersionsInBand(string bandVersion); // "9.0.2xx"
string GetLatestSdkInBand(string bandVersion);
bool IsSdkBandSupported(string bandVersion, DateOnly? asOf = null);
SdkFeatureBand GetLatestSupportedBand(string runtimeVersion);

// CVE workflows
IEnumerable<CveReference> GetCvesForPatch(string patchVersion);
IEnumerable<CveReference> GetCvesForVersion(string majorMinor); // All patches
IEnumerable<PatchRelease> GetPatchesFixingCve(string cveId);
bool HasSecurityFixes(string patchVersion);
IEnumerable<PatchRelease> GetSecurityPatches(string majorMinor);

// Download workflows
DownloadInfo GetLatestSdkDownload(string rid); // "linux-x64"
IEnumerable<DownloadInfo> GetAllDownloads(string version);
```

### Phase 3: Speculative APIs (Future Exploration)

These require validation of use cases and may evolve based on feedback:

```csharp
// Timeline queries
IEnumerable<ReleaseVersion> GetReleasesInDateRange(DateOnly start, DateOnly end);
ReleaseVersion GetReleaseByDate(DateOnly date);

// Comparison queries
ComparisonResult CompareVersions(string version1, string version2);
IEnumerable<string> GetAddedFeatures(string fromVersion, string toVersion);

// Migration planning
MigrationPath GetMigrationPath(string currentVersion, string targetVersion);
IEnumerable<PatchRelease> GetRequiredPatches(string currentPatch, string targetPatch);

// Advanced CVE analytics
CveSummary GetCveSummaryForVersion(string version);
IEnumerable<string> GetVersionsAffectedByCve(string cveId);
```

## Type Families

### Schema OM Family (Serialization-Focused)

Located in `DotNet.Releases.Schema` namespace:

- `ReleaseIndexDocument`
- `ReleaseVersionDocument`
- `ReleaseVersionSummary`
- `SdkIndexDocument`
- `PatchReleaseDocument`
- `HalLink`
- `Lifecycle`
- `ReleaseSchemaContext` (JsonSerializerContext)

### Helper Type Family (Behavior-Focused)

Located in `DotNet.Releases` namespace:

- `ReleaseGraph` (Layers 2 & 3 combined)
- `ReleaseVersion`
- `SdkIndex`
- `SdkFeatureBand`
- `PatchRelease`
- `CveReference`
- `DownloadInfo`

**Composition**: Helper types wrap schema OMs and add navigation/query capabilities.

## Key Implementation Details

### Link Following Pattern

Each helper type exposes typed link accessors:

```csharp
public class ReleaseVersion
{
    private readonly ReleaseVersionSummary _document;
    private readonly ILinkFollower _linkFollower;
    
    // Navigation uses shared cache via ILinkFollower
    public async Task<SdkIndex> GetSdkIndexAsync()
    {
        var link = _document.Links?["sdk-index"];
        var doc = await _linkFollower.FetchAsync<SdkIndexDocument>(link.Href);
        return new SdkIndex(doc, _linkFollower);
    }
}
```

### Sync vs Async APIs

- **Synchronous**: Operations on embedded data already in memory (e.g., `GetSupportedVersions()`)
- **Asynchronous**: Operations requiring link following (e.g., `GetPatchesAsync()`)

Users don't need to understand why; it naturally guides them toward efficient patterns.

### Document Exposure

Helper types always expose their underlying document:

```csharp
public class ReleaseVersion
{
    public ReleaseVersionSummary Document { get; }
    // ... helper methods ...
}
```

This provides an escape hatch for advanced scenarios without abandoning the helper API.

## Example End-to-End Workflows

### Workflow 1: Find Supported Versions

```csharp
var graph = await ReleaseGraph.LoadFromUrlAsync(rootUrl);
var supported = graph.GetSupportedVersions()
    .Where(v => v.ReleaseType == ReleaseType.LTS);

foreach (var version in supported)
{
    Console.WriteLine($"{version.Version} (EOL: {version.EolDate})");
}
```

### Workflow 2: Get Patches with CVEs

```csharp
var graph = await ReleaseGraph.LoadFromUrlAsync(rootUrl);
var v90 = graph.GetVersion("9.0");
var patches = await v90.GetPatchesAsync();

var securityPatches = patches.Where(p => p.HasCves);
foreach (var patch in securityPatches)
{
    Console.WriteLine($"{patch.Version}: {patch.CveCount} CVEs");
}
```

### Workflow 3: SDK Feature Band Support (The Hard Problem)

```csharp
var graph = await ReleaseGraph.LoadFromUrlAsync(rootUrl);
var sdkIndex = await graph.GetLatestSdkAsync();
var supportedBands = sdkIndex.GetSupportedBands();

Console.WriteLine("Currently supported SDK feature bands:");
foreach (var band in supportedBands)
{
    var latest = sdkIndex.GetLatestSdkInBand(band.Version);
    Console.WriteLine($"  {band.Version}: Latest is {latest}");
}
```

## Success Criteria

1. **Cognitive load**: Users discover capabilities through IntelliSense, not documentation
2. **Performance**: Common queries require ≤2 HTTP calls (root + one detail fetch)
3. **Flexibility**: Advanced users can drop to lower layers without friction
4. **Testability**: Each layer can be tested in isolation
5. **Maintainability**: Adding new workflows doesn't require changing core architecture

## Future Considerations

- **Code generation**: Consider generating Layer 1 OMs and Layer 2 link accessors from JSON schemas
- **Alternative formats**: Layer 1 could support XML serialization alongside JSON
- **Offline mode**: Consider bundling release data for scenarios without network access
- **Extensions**: Third-party libraries could add domain-specific workflows (e.g., container image queries)
