# distroessed

> Managing distro versions is distressing.

This repo contains several tools (listed first) and libraries that are used for managing distro lifecycle, packages, and other topics. Some of the tool outputs are documented at [dotnet/core](https://github.com/dotnet/core/blob/main/release-notes/README.md).

## Distroessed

[Distroessed](./src/Distroessed/README.md) produces a JSON document that joins the current state of distro support for a given .NET release, per its `supported-os.json` file and the [endoflife.date](https://endoflife.date) service. It is a useful tool for determining if a `supported-os.json` is inaccurate and needs to be updated.

## GenerateJsonShemas

[GenerateJsonShemas](./src/GenerateJsonSchemas/README.md) produces [JSON schemas](https://github.com/dotnet/core/blob/main/release-notes/schemas/README.md) for a variety of [object models](./src/DotnetRelease/README.md) defined in this repository. It relies on [JsonSchemaExporter](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/libraries#jsonschemaexporter).

## LinuxPackagesMD

[LinuxPackagesMD](./src/LinuxPackagesMd/README.md) produces a markdown document with the set of required (baseline) distro packages for a .NET apps, for a given .NET release, per its `os-packages.md`.

## SupportedOSMD

[SupportedOSMD](./src/SupportedOsMd/README.md) produces a markdown document with the current status of distro support for a given .NET release, per its `supported-os.json` file.

## Libraries

There are multiple libraries in the repo, which are used by the tools.

- [DotnetRelease](./src/DotnetRelease/README.md) -- Defines multiple object models and JSON APIs based on [JSON source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
- [EndofLifeDate](./src/EndOfLifeDate/README.md) -- Defines an object model for [endoflife.data](https://endoflife.date) and a JSON API based on [JSON source generation](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/source-generation).
- [MetadataHelpers](./src/MarkdownHelpers/README.md) -- Provides some simple classes for generating markdown.
