# Testing UpdateIndexes

You can test this tool with `dotnet run`. It requires a clone of richlander/core to run. It is important that any content or tools you download are not added to git history.

## .gitignore

The `.gitignore` for the repo has these lines:

```gitignore
# .NET installation scripts and tools (DO NOT COMMIT) - from dotnet-install-for-agents
_temp/
# .NET SDK installation directory (if installed locally)
.dotnet/
```

That means that these directories are safe to used and will not be added to git history.

## Instructions

You need to clone this repo. Clone it to a location that will not add to git history. The example uses the `_temp` directory.

Clone command: `git clone https://github.com/richlander/core`

## Single Directory Mode (Original Behavior)

The tool can be called with a single directory location, like the following. In this mode, input and output directories are the same:

```bash
dotnet run ~/tmp/core/release-notes
```

## Two Directory Mode (New Feature)

The tool can now be called with separate input and output directories. This enables targeted testing with read-only input and allows multiple tests to run concurrently:

```bash
dotnet run ~/tmp/core/release-notes ~/tmp/test-output
```

In this mode:
- Input directory: `~/tmp/core/release-notes` (read-only access)
- Output directory: `~/tmp/test-output` (will be created if it doesn't exist)

This approach allows you to:
- Keep the original release-notes directory unchanged
- Run multiple concurrent tests with different output directories
- Test the tool's behavior without affecting the source data

## Validation

If making changes for a new root `index.json`, check: `~/tmp/core/release-notes/index.json`

The paths are examples. Update that paths as appropriate.
