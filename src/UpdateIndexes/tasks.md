# Project tasks

The following are the tasks for the project

- [x] Generate version index.json hierarchy
- [x] Generate history index.json hierarchy
- [ ] Define an approach for exposing runtime and SDK version numbers, like we currently have in `release-index.json`. Current plan is to include a `manifest.json` file in each patch version directory (like `8.0/8.0.1`) with dates, version numbers, and CVEs. The patch version index can link to the index.json and manifest.json file as root links as "latest-patch-index" (or similar). This approach doesn't have the same coherency problems as `release-index.json` since the indexes and the content will always be consistent.
- [ ] Consider an index.json at the patch version location. This would enable very quick templated URL access to the monthly location (which will be a smaller file). If we did that, it would motivate removing some of the lower-value files/data from the patch-version index. This is similar to how the history index already works, aligning the two models.

## Schemas

- [ ] Generate schemas for all the OMs with the [GenerateJsonSchemas](../GenerateJsonSchemas/) tool
- [ ] Publish schemas to the `release-notes/schemas` directory
- [ ] Reference schemas from the generated JSON documents so that it is easy for assistants to access the documents for more information in context of reading a given document
- [ ] Document the object models (to enable rich schema information)
- [ ] Align object models between version and history schemas. The naming and structure should be very similar.
- [ ] The type name `ReleasesIndex` is likely wrong given that it is for the version index.

## .NET SDK links

## Longer-term

- [ ] Define a declarative way to project high-value data up so that it doesn't need to be hard-coded. However, if such a facility requires using Reflection (not AOT compatible, then we won't do it).
