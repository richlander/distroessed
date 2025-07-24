# Distroessed - .NET Release Data Management Tools

This repository contains a collection of applications and libraries for managing, generating, and processing .NET release data, security advisories (CVEs), and documentation.

## Applications

### Core Index Generation
- **UpdateIndexes** - Primary tool that generates HAL+JSON release indexes, llms.txt files, and cross-references all release data
- **GenerateJsonSchemas** - Creates JSON schemas for all data models to enable validation and tooling support

### CVE (Security Advisory) Processing
- **CveIndex** - Generates monthly CVE index files from release notes directory structure
- **CveIndexMarkdown** - Creates markdown versions of CVE index files for human consumption  
- **CveMarkdown** - Converts individual CVE JSON files to markdown using templates
- **CveValidate** - Validates CVE data consistency and completeness
- **CheckCvesForReleases** - Cross-references CVEs against release data to ensure accuracy

### Release Documentation Generation
- **UpdateReleasesMarkdown** - Generates and updates release notes markdown files
- **SupportedOsMd** - Creates supported OS documentation from structured data
- **LinuxPackagesMd** - Generates Linux package information documentation
- **ReleaseReport** - Creates comprehensive release reports and statistics

### Data Access and Utilities
- **Distroessed** - CLI tool for querying supported OS information by .NET version
- **DotnetInfo** - Fetches and displays active major release information from official APIs
- **MarkdownTemplateTest** - Testing utility for markdown template processing

### Development Tools
- **Test** - General testing and development utility
- **DistroessedExceptional** - Specialized testing tool (purpose needs clarification)

## Libraries

### Core Data Models
- **DotnetRelease** - Central library containing all data models, serialization contexts, and core business logic
  - HAL+JSON data models for hypermedia APIs
  - Legacy data models for backward compatibility  
  - Release information models and enums
  - CVE and security advisory models
  - OS package and support matrix models

### Utility Libraries
- **FileHelpers** - Adaptive path handling for local files, web URLs, and cross-platform compatibility
- **MarkdownHelpers** - Utilities for markdown generation including tables, links, and formatting
- **MarkdownTemplate** - Template processing engine for generating markdown from templates
- **JsonSchemaInjector** - Adds JSON schema references to generated JSON files
- **EndOfLifeDate** - Date calculations and utilities for .NET release lifecycle management

## Consolidation Opportunities

Based on the analysis, several consolidation opportunities exist:

### CVE Tool Consolidation
The CVE-related applications could potentially be consolidated:
- **CveIndex** and **CveIndexMarkdown** share similar logic and could be merged into a single tool with output format options
- **CveMarkdown** and **CveValidate** could be combined into a comprehensive CVE processing tool
- **CheckCvesForReleases** could be integrated into the main CVE validation workflow

### Documentation Generation Consolidation  
Several documentation generators could be unified:
- **UpdateReleasesMarkdown**, **SupportedOsMd**, and **LinuxPackagesMd** all generate markdown from structured data
- These could become modules within a single documentation generation tool

### Testing Tool Consolidation
- **Test** and **MarkdownTemplateTest** could be merged
- **DistroessedExceptional** purpose should be clarified - may be redundant

### Potential Unified Architecture
Consider creating:
1. **DotnetReleaseManager** - Unified CLI with subcommands for all major operations
2. **DotnetReleaseGenerator** - Consolidated documentation and index generation
3. **DotnetReleaseCve** - Unified CVE processing pipeline

## Getting Started

1. Build the solution: `dotnet build src/src.sln`
2. Run the main index generation: `UpdateIndexes <release-notes-directory>`
3. Generate schemas: `GenerateJsonSchemas <target-directory>`
4. Process CVEs: `CveIndex <release-notes-directory>`

## Architecture Notes

The system follows a pipeline architecture:
1. **Data Collection** - Tools read from various sources (file system, web APIs)
2. **Processing** - Core libraries transform and validate data
3. **Generation** - Applications output indexes, documentation, and reports
4. **Validation** - Tools ensure data consistency and completeness

The **DotnetRelease** library serves as the central hub, providing shared data models and business logic across all applications.