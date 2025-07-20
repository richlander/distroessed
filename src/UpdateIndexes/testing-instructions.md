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

The tool can be called with the cloned location, like the following.

```bash
dotnet run _temp/core/release-notes`
```

If making changes for a new root `index.json`, check: `_temp/core/release-notes/index.json`

The paths are examples. Update that paths as appropriate.
