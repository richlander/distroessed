# Commit-Specific Links Workflow

This document describes how to generate index files with commit-specific links to avoid GitHub's aggressive caching when sharing with LLMs.

## Problem

GitHub's raw content CDN (`raw.githubusercontent.com`) aggressively caches content. When testing with LLMs that read and analyze content, they may see stale cached data instead of your latest changes.

## Solution

Generate index files with commit-specific links instead of branch-based links. This is done in two steps:

1. **First generation**: Generate indexes with regular `main` branch links and commit
2. **Second generation**: Regenerate indexes using the first commit's SHA, then commit again

The second commit becomes the "cache-busting" version you share with LLMs.

## Two-Step Workflow

### Step 1: Initial Generation and Commit

```bash
# Generate indexes with default 'main' branch links
cd ~/git/core-rich/release-notes
dotnet run --project ~/git/distroessed/src/ShipIndex -- .
dotnet run --project ~/git/distroessed/src/VersionIndex -- .

# Commit the changes
git add .
git commit -m "Update release indexes"
git push

# Note the commit SHA
COMMIT1=$(git rev-parse HEAD)
echo "First commit: $COMMIT1"
```

### Step 2: Regenerate with Commit SHA

```bash
# Regenerate indexes using the commit SHA from step 1
dotnet run --project ~/git/distroessed/src/ShipIndex -- . --commit $COMMIT1
dotnet run --project ~/git/distroessed/src/VersionIndex -- . --commit $COMMIT1

# Commit the updated indexes
git add .
git commit -m "Update indexes with commit-specific links"
git push

# Note this commit SHA - this is what you share
COMMIT2=$(git rev-parse HEAD)
echo "Second commit (share this): $COMMIT2"
```

### Step 3: Share with LLMs

When sharing with LLMs, use links based on the second commit:

```
https://raw.githubusercontent.com/richlander/core/${COMMIT2}/release-notes/archives/index.json
```

## How It Works

- **ShipIndex** and **VersionIndex** both accept an optional `--commit <sha>` parameter
- When provided, all generated links use that commit SHA instead of `main`
- The commit-specific URL format is: `https://raw.githubusercontent.com/richlander/core/{sha}/release-notes/{path}`
- This bypasses GitHub's cache and ensures fresh content

## Example Commands

```bash
# Generate with default 'main' branch
ShipIndex ~/git/core-rich/release-notes

# Generate with specific commit
ShipIndex ~/git/core-rich/release-notes --commit abc123def456

# Generate with separate output directory
ShipIndex ~/git/core-rich/release-notes /tmp/output --commit abc123def456
```

## Notes

- The `--commit` parameter works with any valid Git ref (commit SHA, tag, branch)
- However, using commit SHAs is recommended for maximum cache-busting effectiveness
- The two-step process ensures that all internal links within the indexes are consistent
- You can automate this with a script if needed
