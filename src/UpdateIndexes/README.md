# UpdateIndexes tool

This tool generates JSON indexes and other files for .NET documentation.

Design principles:

- The core algorithm should be general, allowing for generating content in multiple formats (and added over time) from the same source data (this is a lowering-style approach).
- Algorithm should start with leaf nodes and then generate trunk (including root) files, projecting a subset of its data up. The leaf node docs should already exist and the trunk files should be generated.
- The algorithm should assume the structure of the dotnet/core repo, particularly the [`release-notes`](https://github.com/dotnet/core/tree/main/release-notes) directory.
- The final data will be served via a CDN with largely unmanageable differing TTLs per file, presenting the risk of data consistency. As a result, the data projected up must be slow moving and not of great concern if there are mis-matches across files for a short period (TTL length), like the support phase transitioning from active to maintenance. Patch versions, however, are too dangerous to replicate (must be single source).
- The leaf node data can be in plain JSON formats. The trunk data should in the [HAL-JSON](https://en.wikipedia.org/wiki/Hypertext_Application_Language) format to enable simpler machine traveral to the desired data.
