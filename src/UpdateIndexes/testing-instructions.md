# Testing UpdateIndexes

You can test this tool with `dotnet run`. It requires a clone of richlander/core to run. It is important that you maintain your own copy so that you don't interfere with mine. After cloning and testing, you should validate that your copy is clean (no extra changes) and that it is up-to-date with the remote.

Clone command: `git clone https://github.com/richlander/core`

The tool can be called with the cloned location, like the following.

```bash
dotnet run ~/tmp/core/release-notes`
```

If making changes for a new root `index.json`, check: `~/tmp/core/release-notes/index.json`

Hope that helps with testing.
