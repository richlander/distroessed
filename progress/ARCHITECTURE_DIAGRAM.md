# CVE Handling Architecture Diagram

## Before Implementation

```
┌─────────────────────────────────────────────────────────────┐
│                     ShipIndex Tool                          │
├─────────────────────────────────────────────────────────────┤
│ - Loads cve.json directly                                   │
│ - Inline CVE transformation (70+ lines)                     │
│ - Generates month indexes with disclosures                  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              release-history/2024/05/                       │
├─────────────────────────────────────────────────────────────┤
│  index.json                                                 │
│  ├── disclosures: [                                         │
│  │   { id, problem, cvss, timeline, ... }  ← Full details  │
│  │ ]                                                        │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│                    VersionIndex Tool                        │
├─────────────────────────────────────────────────────────────┤
│ - Reads CVE data from summaries                             │
│ - Inline CVE summary creation                               │
│ - Generates major version index only                        │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                      9.0/index.json                         │
├─────────────────────────────────────────────────────────────┤
│  releases: [                                                │
│    {                                                        │
│      version: "9.0.0",                                      │
│      cve-records: [                                         │
│        { id, title, links, ... }  ← Summaries only         │
│      ]                                                      │
│    }                                                        │
│  ]                                                          │
└─────────────────────────────────────────────────────────────┘

PROBLEMS:
❌ Code duplication (CVE transformation in both tools)
❌ Inconsistent formats (disclosures vs cve-records)
❌ Different graph depths (month-level vs major-level)
❌ No symmetry between tools
```

## After Implementation

```
                    ┌─────────────────────┐
                    │   CveHandler Lib    │
                    ├─────────────────────┤
                    │  - CveLoader        │
                    │  - CveTransformer   │
                    └─────────────────────┘
                            ↑       ↑
                            │       │
              ┌─────────────┘       └──────────────┐
              │                                     │
┌─────────────┴───────────────┐     ┌──────────────┴─────────────┐
│      ShipIndex Tool         │     │    VersionIndex Tool        │
├─────────────────────────────┤     ├────────────────────────────┤
│ Uses CveLoader              │     │ Uses CveLoader             │
│ Uses CveTransformer         │     │ Uses CveTransformer        │
│ ~70 lines removed           │     │ Generates patch indexes    │
└─────────────────────────────┘     └────────────────────────────┘
              ↓                                    ↓
              │                                    │
┌─────────────┴───────────────┐     ┌──────────────┴─────────────┐
│ release-history/2024/       │     │         9.0/               │
├─────────────────────────────┤     ├────────────────────────────┤
│ index.json                  │     │ index.json                 │
│ ├── cve-records: [          │     │ ├── cve-records: [         │
│ │   "CVE-2024-001",         │     │ │   "CVE-2024-001",        │
│ │   "CVE-2024-002"          │     │ │   "CVE-2024-002"         │
│ │ ]  ← IDs only             │     │ │ ]  ← IDs only            │
│ │                           │     │ │                          │
│ └── 05/                     │     │ ├── 9.0.0/                │
│     └── index.json          │     │ │   └── index.json         │
│         ├── disclosures: [  │     │ │       ├── disclosures: [ │
│         │   { full CVE }    │     │ │       │   { full CVE }   │
│         │ ]  ← Full details │     │ │       │ ]  ← Full details│
└─────────────────────────────┘     └─│───────────────────────────┘
                                      │
                                      └── 9.0.1/
                                          └── index.json
                                              ├── disclosures: [
                                              │   { full CVE }
                                              │ ]  ← Full details

BENEFITS:
✅ Shared code (no duplication)
✅ Consistent format (disclosures everywhere)
✅ Same graph depth (summary → detail)
✅ Symmetry between tools
✅ Easier to maintain and extend
```

## Data Flow

### Loading CVE Data
```
┌──────────────┐
│  cve.json    │
│  (on disk)   │
└──────┬───────┘
       │
       ↓
┌──────────────────────────┐
│  CveLoader.LoadAsync()   │
└──────┬───────────────────┘
       │
       ↓
┌──────────────────────┐
│    CveRecords        │
│  (full disclosures)  │
└──────────────────────┘
```

### Transforming CVE Data
```
┌──────────────────────┐
│    CveRecords        │
│  (full disclosures)  │
└──────┬───────────────┘
       │
       ↓
┌──────────────────────────────────┐
│  CveTransformer.ToSummaries()    │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────────────┐
│  List<CveRecordSummary>      │
│  (for embedding in indexes)  │
└──────────────────────────────┘

       OR

┌──────────────────────────────────┐
│  CveTransformer.ExtractCveIds()  │
└──────┬───────────────────────────┘
       │
       ↓
┌──────────────────────┐
│   List<string>       │
│   (CVE IDs only)     │
└──────────────────────┘
```

### Filtering CVE Data
```
┌──────────────────────┐     ┌──────────────┐
│    CveRecords        │     │  Release ID  │
│  (all versions)      │     │  "9.0.0"     │
└──────┬───────────────┘     └──────┬───────┘
       │                            │
       └────────────┬───────────────┘
                    ↓
       ┌────────────────────────────────┐
       │  CveTransformer.FilterByRelease()
       └────────────┬───────────────────┘
                    │
                    ↓
       ┌────────────────────────┐
       │    CveRecords          │
       │  (9.0.0 only)          │
       └────────────────────────┘
```

## Index Hierarchy

### Version Index (VersionIndex Tool)
```
index.json (root)
├── 9.0 (major)
│   └── index.json
│       ├── cve-records: ["CVE-2024-001", ...]  ← Summary
│       └── releases:
│           ├── 9.0.0
│           │   ├── cve-records: ["CVE-2024-001"]
│           │   └── _links → 9.0.0/index.json
│           └── 9.0.1
│               ├── cve-records: ["CVE-2024-002"]
│               └── _links → 9.0.1/index.json
│
├── 9.0/9.0.0/index.json (patch detail) ← NEW!
│   └── disclosures: [{ full CVE }]  ← Full details
│
└── 9.0/9.0.1/index.json (patch detail) ← NEW!
    └── disclosures: [{ full CVE }]  ← Full details
```

### History Index (ShipIndex Tool)
```
release-history/
├── 2024/
│   └── index.json (year)
│       ├── cve-records: ["CVE-2024-001", ...]  ← Summary
│       └── months:
│           └── 05
│               └── _links → 05/index.json
│
└── 2024/05/index.json (month detail)
    └── disclosures: [{ full CVE }]  ← Full details
```

## Symmetry Achieved

```
VersionIndex               ShipIndex
═══════════               ══════════

Major (9.0)        ←→     Year (2024)
  ├─ CVE IDs only           ├─ CVE IDs only
  └─ Links to patches       └─ Links to months

Patch (9.0.0)      ←→     Month (2024/05)
  └─ Full CVE details       └─ Full CVE details
```

## Key Improvements

1. **Code Reuse**: Single transformation logic
2. **Consistency**: Same data format everywhere
3. **Symmetry**: Parallel structure in both tools
4. **Maintainability**: Changes in one place
5. **Extensibility**: Easy to add features
