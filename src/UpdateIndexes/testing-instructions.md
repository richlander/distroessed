# Testing UpdateIndexes

You can test this tool with `dotnet run`. It requires a clone of richlander/core to run. It is important that you maintain your own copy so that you don't interfere with mine. After cloning and testing, you should validate that your copy is clean (no extra changes) and that it is up-to-date with the remote.

Clone command: `git clone https://github.com/richlander/core`

The tool can be called with an input directory, or with both input and output directories:

## Single directory mode (input and output are the same):
```bash
dotnet run ~/tmp/core/release-notes
```

## Dual directory mode (input read-only, separate output directory):
```bash
dotnet run ~/tmp/core/release-notes ~/tmp/test-output
```

The dual directory mode enables targeted testing while treating the input as read-only. This allows multiple tests to be run on the input `release-notes` directory (and the containing repo) at once without modifying the original data.

If making changes for a new root `index.json`, check:
- Single directory mode: `~/tmp/core/release-notes/index.json`
- Dual directory mode: `~/tmp/test-output/index.json`

Hope that helps with testing.
