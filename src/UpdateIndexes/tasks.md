# Project tasks

The following are the tasks for the project

## Rules

- All code needs to be Native AOT friendly.
- Use the following root URL for assets: `https://raw.githubusercontent.com/richlander/core/main/release-notes`
- Schemas should use this root URL not `json-schema.org` or similar.
- Only generate schemas and JSON files that are asked for.
- Leave the `TargetFramework` and `LanguageVersion` property as-is. If it needs to be changed, ask first and explain why.
- If you need to run the `UpdateIndexes` tool, it needs a target directory as input.
  - On macOS, use: `/Users/rich/git/core-rich/release-notes`
  - On Linux, use: `/home/rich/git/rich-core/release-notes`

## JSON format

- [ ] Consider an index.json at the patch version location. This would enable very quick templated URL access to the monthly location (which will be a smaller file). If we did that, it would motivate removing some of the lower-value files/data from the patch-version index. This is similar to how the history index already works, aligning the two models.
- [ ] Define an approach for exposing runtime and SDK version numbers, like we currently have in `release-index.json`. Current plan is to include a `manifest.json` file in each patch version directory (like `8.0/8.0.1`) with dates, version numbers, and CVEs. The patch version index can link to the index.json and manifest.json file as root links as "latest-patch-index" (or similar). This approach doesn't have the same coherency problems as `release-index.json` since the indexes and the content will always be consistent.

## Schemas

- [ ] Generate schemas for all the HalJson OMs with the [GenerateJsonSchemas](../GenerateJsonSchemas/) tool.
- [ ] Document the object models (to enable rich schema information)

## Object models (OM)
- [ ] Change `HistoryIndex` to `ReleaseHistoryIndex`. The filename and the child types should follow.
- [ ] Change `ReleaseIndex` to `ReleaseVersionIndex`. The filename and the child types should follow.
- [ ] Move URL information from the ReleaseNotes class in the DotnetRelease project to a new Location class.
- [ ] The GitHubBaseUri property should change from the substring "dotnet/core" to "richlander/core". That property should be used wherever the desired root URL (mentioned above) is needed. This will help avoid DRY problems.

## Tools

- [ ] Remove setting "args" in UpdateIndexes/Program.cs. That was a debugging technique on Linux, isn't compatible on other OSes, and isn't needed anymore.
- [ ] Write a script in the root `scripts` folder that generates schemas. It should take a target directory.
- [ ] Run the schema script with the `schemas` directory (at root of the repo) as the target.
- [ ] Add a new library that uses JsonDocument to surgically open any JSON file and add a "$schema" property at root (in the typical location).
- [ ] Update the UpdateIndexes tool to use this new library to add schema references to all the generated JSON documents so that it is easy for assistants access schema information (to better undertand the meaning of properies).


## .NET SDK links

- None yet

## Longer-term

Note: These tasks are to consider for later. Don't work on this now.

- [ ] Define a declarative way to project high-value data up so that it doesn't need to be hard-coded. However, if such a facility requires using Reflection (not AOT compatible, then we won't do it).
