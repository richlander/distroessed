# Update Index tool

The Update Indexes tool generates [HAL+JSON](https://www.ietf.org/archive/id/draft-kelly-json-hal-11.html) style JSON documents that tools and chat assistants can follow to get rich information about .NET product releases from [.NET release notes](https://github.com/dotnet/core/tree/main/release-notes).

This document is a spec. Backlog tasks are listed in [tasks.md](tasks.md). Schemas are describes in the [DotnetRelease](../DotnetRelease/) project, with the relevant ones being in the [HalJson](../DotnetRelease/HalJson/) directory.

There are two big ideas at play here:

- Our release notes contain a lot of valuable information that would benefit from a more rigorous design, an expansive cross-referencing index graph, and access to it via smaller files. In the current scheme, you quickly hit 1MB+ of JSON.
- AI assistants and agents greatly value token-density and are capable of using tools to follow links. Graph navigation is a fundamental AI skill. AI tools will be happy to explore a [hypermedia graph](https://en.wikipedia.org/wiki/Hypermedia) to find information if it is an efficient and thoughtful approach. We carefully build tools that are delightful for humans to use; we can do the same for AI tools.

## Motivation

We host release content on a CDN. We've learned that each file has its own cache/TTL lifetime. It's possible to control TTL with [cache purging](https://techdocs.akamai.com/purge-cache/docs/welcome-purge). That can be considered OK if its required but should never be a substitute for designs that don't demand that level of constant care and feeding.

The [`release-index.json`](https://raw.githubusercontent.com/dotnet/core/refs/heads/main/release-notes/releases-index.json) is a great example of this challenge. It's an overachieving file, acting as an index and advertising high-value content upwards (projected up from the patch version files). On one hand, this is a great design, offering a one-stop shop. Many users can read just one file and then are done. On the other, it's a subtle design trap to avoid.

The problem is with users that read versions numbers from `release-index.json` and then expect them to be consistent with deserialized dictionaries that they create from downstream JSON files (referenced from the index). TTLs are hostile to that approach since they produce data inconsistencies, even if the origin web server / storage account is consistent.

Another problem is that there are (presumably) millions of hits a month on this file, since it is the root of our JSON release files system. However, we don't handle it with the care that it deserves. If you look at the [commit history](https://github.com/dotnet/core/commits/main/release-notes/releases-index.json), you can see that it is edited multiple times per month, even just for preview releases. That's the opposite of how a production system should operate. Instead, our release index root file should be updated exactly once a year given that we ship major releases annually.

The value of offering up-to-date version numbers to quick-one-file-look users pales in comparison to the value of having a highly-cacheable super-reliable root index. The make this point abundantly clear: on machines where the file is already cached, you can read it and are still effectively at file zero. The goal isn't about offering the shortest LOC to get the information but building reliable systems. The typical webpage is multi-request and this system can be, too.

This project establishes a new index scheme that indends to avoid existing design traps. There are other problems with the approach used by `release-index.json`, however, they will be covered in context of the intended design.

## Approach

The tool should generate two directory hives of index and related JSON content:

- .NET versions (e.g what are the major .NET versions and the patch releases for each).
- .NET release history (e.g. which runtime and SDK versions did we ship in June 2025).

Principles:

- Users may start with a runtime version, an SDK version, or a date (like a month). Each of those should be good starting points to get good information.
- The primary targeted user for this information schema is chat assistants. If they are effective for that audience, it is likely they will be very good for other users.
- It is more important to offer cache-friendly documents than to include high-value information (like the latest patch for a major release) and instead to require readers to make a second network request to retreive a document that has that information.

General:

- Generated content lives in the `release-notes` directory in the `core` repo.
- All index files will be called `index.json`.
- Version content lives directly in `release-notes` directory, in `index.json`
- The index schema will differ between uses (e.g. major version index vs patch version index).
- Each index.json only contains summary-level metadata and links to the next level, not full nested data for all descendants.
- The HAL+JSON scheme will be used consistently.
- The HAL+JSON model enables projecting information upwards into the indexes. In general, high-value stable information should be projected up as far as it remains coherent, even if it is duplicated in multiple levels of the index.
- The `kind` root property will be consistently present to indicate to the reader what schema to expect.
- Schema links should be present in JSON documents, with descriptive property annotations. The links should follow the same URL schema as the `href` properties in the schema.

The index.json at each level contains only summary information (such as version, title, and links to the next index), rather than a full recursive listing of all contained data. For example, a major version index lists a link to a patch version index, but does not provide an embedded set of patch version numbers.

It's possible for a chat assistant to "get an idea in their head" about how this data should be structured and then running with that. If it doesn't find what it expects, it may get confused. Enabling more than one path through the data makes it compatible with a wider variety of ideas that a given chat assistant might latch onto. Requiring an AI to take a single brittle path is unlikely to be successful, particularly if alternate conceptual paths are equally reasonable. It then becomes a tension between what the AI guesses your format looks like and what you decided to materialize in your format. Instead a rich format with liberal cross-referencing is more likely to be successful. It may be the case that a richer format is more likely to get a chat assistant to accept what's on offer.

In early experience with chat assistants accessing these files, I learned that chat assistants may expect index files to go deeper into the hierarchy than is necessary while at the same time appreciating high-level information being projected quite high. This is an example of altering structure based on observed patterns. It's also possible for a single assistant to be multi-modal, switching patterns across runs. The availability of reasoning and other breadcrumb text makes it straightforward to follow what meaning and assumptions they make and then adapting structure to accomodate.

Here's the root index, demonstrating the complete structure.

```json
{
  "$schema": "https://raw.githubusercontent.com/richlander/core/main/release-notes/schemas/major-version-index.json",
  "kind": "index",
  "description": "Index of .NET major versions",
  "_links": {
    "self": {
      "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/index.json",
      "relative": "index.json",
      "title": ".NET Release",
      "type": "application/hal\u002Bjson"
    }
  },
  "_embedded": {
    "releases": [
      {
        "version": "10.0",
        "kind": "index",
        "_links": {
          "self": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/10.0/index.json",
            "relative": "10.0/index.json",
            "title": ".NET 10.0",
            "type": "application/hal\u002Bjson"
          },
          "manifest": {
            "href": "https://raw.githubusercontent.com/richlander/core/main/release-notes/10.0/manifest.json",
            "relative": "10.0/manifest.json",
            "title": "Manifest",
            "type": "application/hal\u002Bjson"
          }
        },
        "support": {
          "release-type": "sts",
          "phase": "preview",
          "ga-date": "2025-11-11T00:00:00+00:00",
          "eol-date": "2028-11-14T00:00:00+00:00"
        }
      },
```

The combination of `$schema`, `kind` and `description` provides a complete description of the kind of data to be expected in the index. The liberal use of `_links` and `_embedded` provides familiar footing within an unfamiliar domain language.

## Cache-Friendly Design Philosophy

All index files include the note "This file is cache-friendly; follow links for the most current details." in their descriptions. This reflects a core design principle:

- **Index files are designed to be nearly immutable** and highly cacheable, with long TTLs
- **Frequently changing details** (like latest-supported-version or next-eol-date) are not embedded at the top level
- **Clients should follow links** to more granular resources for the most current information
- **This approach prioritizes system reliability** over convenience for single-file consumers

This design avoids the cache invalidation problems described in the Motivation section while still providing efficient navigation for AI assistants and other tools that can follow hypermedia links.

The .NET 10 support information is projected up, in the `support` property. Notably, this information is very slow changing and won't suffer from the same data coherency problem covered earlier. The only property that would be expected to change over the life of the release would be `phase` changing from `preview` through a set of values ending with `eol`. It would once every several months. The rest of the data would not change.

The .NET 10 `release` object within the `releases` array can be thought of multiple ways:

- It's a reference to a hypermedia document.
- It's a hypermedia document that describes a references to another hypermedia document.
- The reference and the target are the same hypermedia document, with the one (conceptually) above being a partial view with a link to the canonical definition if more information is needed.

I subscribe to this last definition. There is a (virtuously) loose definition of where data can and should be presented. There is no expectation that all the data be resident in HAL+JSON documents. It is fine and expected for a link to offer a plain JSON or Markdown that an AI or other tool is expected to process.

## Version hive

The version hive describes the domain of .NET versions.

Note: call out root type for each.

Root / Major version index:

- Type: `ReleaseIndex`
- The `index.json` in the root of `release-notes` lists and references all .NET major versions.
- The root `_links` property is self-referential to `index.json`.
- .NET major versions are described in the root `_embedded` property with `ReleaseIndexEmbedded` type.
- `ReleaseIndexEmbedded` includes an array of `ReleaseIndexEntry` objects, one per major release.
- Within each release object, the links are to patch version index files in each version directory, like `10.0/index.json`.
- Release objects also include links to other high-value content like `manifest.json`, which includes extra lifecycle information (e.g. GA and EOL dates, release type).
- Some of this high-value data from `manifest.json` is projected up to `index.json`.

Patch version index:

- Type: `ReleaseIndex`
- The `index.json` in each major version directory lists and references all patch versions for a given major release.
- The root `_links` property is self-referential to `index.json` and also references all known markdown and JSON files in the root of each version directory. Only known files are listed so that a nice description can be offered.
- Patch versions are described in the root `_embedded` property with the `ReleaseIndexEmbedded` type.
- `ReleaseIndexEmbedded` includes an array of `ReleaseIndexEntry` objects, one per patch release.
- Within each release object, the links are to high-value JSON and markdown content like `release.json` and `cve.json`.
- Release objects also include links to other high-value content like `manifest.json`, which includes extra lifecycle information (e.g. GA and EOL dates, release type).
- Some of this high-value data from `manifest.json` is projected up to `index.json`.

Note: `releases.json` is highly valuable for some scenarios but is huge. We should ensure that it isn't exposed too high in the hiearchy to avoid it being accessed w/o first considering other sources. A crazy idea would be only exposing it in the manifest file. `release.json` is similar, but is a lot smaller. It still may be too large for some workflows. It, however, should definitely be exposed in an idex file.

## SDK hive

.NET SDK versions should be just as easy to discover as runtime versions. They are a very different version domain, both because the version numbers look different and there can be multiple supported SDK families at once.

The 8.0.18 release is a good example.

8.0.18 release notes: <https://raw.githubusercontent.com/dotnet/core/refs/heads/main/release-notes/8.0/8.0.18/8.0.18.md>

It documents and offers three SDK versions.

- 8.0.412
- 8.0.315
- 8.0.118

These are from 3 ddifferent features bands, 8.0.1xx, 8.0.3xx, and 8.0.4xx. We can conclude that 8.0.2xx is out of support and no longer produced.

Each major .NET version should have an `sdk` directory with and index.json that describes the available feature bands, including their support status. Each feature band should have its own directory, like `8.0.1xx`. This directory should include an index.json file that lists all of the patches for that feature band, like 8.0.118.

These are the files in the `8.0/8.0.18` directory.

```bash
$ ~/git/core/release-notes$ ls 8.0/8.0.18
8.0.118.md  8.0.18.md  8.0.315.md  release.json
```

The `8.0.118.md` and `8.0.315.md` files are obvious. There is no matching `8.0.412` file. That's because this SDK is the latest and is included with the runtime file in `8.0.18`.

The following files are expected (these are all examples):

- `8.0/index.json` -- Describes a major release. Links to `8.0/sdk/index.json` with a descriptive link about .NET 8.0 SDK release information.
- `8.0/sdk/index.json` -- Describes a major release, but specific to SDK releases and includes feature bands as its release objects. Links to `index.json` files in the feature band directories for the major release. Each feature band has the same lifecycle content as the major releases in the root index.json.
- `8.0/sdk/8.0.1xx/index.json` -- Describes a feature band and includes specific SDK patch releases within that feature band as its release objects. Links to index.json files in a patch directory. Each release object links to a markdown release file, like the ones listed above.
- `8.0/sdk/8.0.1xx/8.0.118/index.json` -- Describes an SDK patch release and the markdown release file that describes it. Also links to the index.json file for the runtime that it is associated with like `8.0/8.0.18/index.json`.

There are convenience short-links for SDKs.

Here is an example of file that descibes them:

```json
{
    "component": "sdk",
    "version": "8.0",
    "label": ".NET SDK 8.0",
    "support-phase": "active",
    "hash-algorithm": "sha512",
    "files": [
        {
            "name": "dotnet-sdk-linux-arm.tar.gz",
            "type": "tar.gz",
            "rid": "linux-arm",
            "os": "linux",
            "arch": "arm",
            "url": "https://aka.ms/dotnet/8.0/dotnet-sdk-linux-arm.tar.gz",
            "hashUrl": "https://aka.ms/dotnet/8.0/dotnet-sdk-linux-arm.tar.gz.sha512"
        },
        {
            "name": "dotnet-sdk-linux-arm64.tar.gz",
            "type": "tar.gz",
            "rid": "linux-arm64",
            "os": "linux",
            "arch": "arm64",
            "url": "https://aka.ms/dotnet/8.0/dotnet-sdk-linux-arm64.tar.gz",
            "hashUrl": "https://aka.ms/dotnet/8.0/dotnet-sdk-linux-arm64.tar.gz.sha512"
        },
```

I have included some templates in `8.0/_sdk` to show the form. However, the tool should generate these link files per major release.

```bash
rich@merritt:~/git/core-rich/release-notes$ ls 8.0/_sdk/
sdk-8.0-1xx.json  sdk-8.0.3xx.json  sdk-index.json
sdk-8.0.2xx.json  sdk-8.0.4xx.json  sdk.json
```

There are link files per feature band. `sdk.json` offers links to the latest feature band. That is what is displayed above. These files are in a "plain json" format not HAL+JSON. I think plain json makes the most sense for these. I expect lots of users for these beyond HAL readers.

I also index.json in that directory with some earlier thinking on what a root index.json could look like. It doesn't entirely agree with the requirements above or match `8.0/index.json`. We need to merge the ideas.

So far, I've focused on 8.0. We need to apply this scheme to other versions. However, the links only work for 8.0+. We should not include them in the code we generate for .NET 7 and earlier. Everything else can be applied for all versions.
