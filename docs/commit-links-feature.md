# Commit-Specific Links Feature

## Summary

Added support for generating index files with commit-specific GitHub links to avoid caching issues when sharing content with LLMs.

## Changes Made

### 1. Core Infrastructure (`src/DotnetRelease/Location.cs`)
- Added `SetGitHubCommit(string commitSha)` method
- Allows runtime configuration of the GitHub base URI with a specific commit SHA
- Default remains `main` branch if not specified

### 2. ShipIndex Tool (`src/ShipIndex/Program.cs`)
- Added `--commit <sha>` command-line parameter
- Updated argument parsing to handle the new option
- Calls `Location.SetGitHubCommit()` when commit is provided

### 3. VersionIndex Tool (`src/VersionIndex/Program.cs`)
- Added `--commit <sha>` command-line parameter
- Updated argument parsing to handle the new option
- Calls `Location.SetGitHubCommit()` when commit is provided

### 4. Documentation
- **`docs/commit-links-workflow.md`**: Detailed workflow guide
- **`src/ShipIndex/README.md`**: Updated usage documentation
- **`src/VersionIndex/README.md`**: Updated usage documentation

### 5. Automation Script
- **`scripts/generate-indexes-with-commit.sh`**: Bash script to automate the two-step workflow

## Usage

### Simple Usage (Default Behavior)
```bash
# Uses 'main' branch in links (existing behavior)
ShipIndex ~/git/core-rich/release-notes
VersionIndex ~/git/core-rich/release-notes
```

### With Commit SHA (Cache-Busting)
```bash
# Uses specific commit in links
ShipIndex ~/git/core-rich/release-notes --commit abc123def456
VersionIndex ~/git/core-rich/release-notes --commit abc123def456
```

### Automated Two-Step Workflow
```bash
# Runs both steps automatically
scripts/generate-indexes-with-commit.sh ~/git/core-rich/release-notes
```

## How It Works

1. **Normal Links** (default):
   ```
   https://raw.githubusercontent.com/richlander/core/main/release-notes/archives/index.json
   ```

2. **Commit-Specific Links** (with `--commit abc123`):
   ```
   https://raw.githubusercontent.com/richlander/core/abc123/release-notes/archives/index.json
   ```

The commit-specific URLs bypass GitHub's aggressive caching and ensure LLMs always read the exact version you intend.

## Two-Step Workflow Rationale

The problem: You don't know the commit SHA before committing the content.

The solution: A two-step process:
1. **First pass**: Generate with `main`, commit → Get commit SHA #1
2. **Second pass**: Regenerate with SHA #1, commit → Get commit SHA #2
3. **Share**: Use commit SHA #2 for LLM access

This ensures all internal links within the indexes are consistent and point to the correct commit.

## Benefits

- **Cache-busting**: LLMs always see fresh content
- **Reproducibility**: Specific commits provide stable, immutable references
- **Backward compatible**: Default behavior unchanged (uses `main`)
- **Simple integration**: Just add `--commit <sha>` to existing commands
- **Automated**: Script handles the entire workflow

## Testing

Both tools have been updated and tested:
- ✅ Build successfully
- ✅ Help text displays correctly
- ✅ Backward compatible with existing usage
- ✅ Commit parameter properly modifies generated links
