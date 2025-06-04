using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using DotnetRelease;
using System.Runtime.InteropServices;

namespace DotnetInfo;

public class DotnetReleaseInfo(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;
    MajorReleasesIndex? _majorReleasesIndex = null;

    public async Task<MajorReleasesIndex> GetMajorReleasesIndexAsync()
    {
        if (_majorReleasesIndex != null)
        {
            return _majorReleasesIndex;
        }

        var stream = await _httpClient.GetStreamAsync(ReleaseNotes.MajorReleasesIndexUri)
            ?? throw new InvalidOperationException("Failed to retrieve major releases index stream.");
        var index = await ReleaseNotes.GetMajorReleasesIndex(stream)
            ?? throw new InvalidOperationException("Failed to retrieve major releases index.");
        _majorReleasesIndex = index;
        return index;
    }

    public async Task<IEnumerable<MajorReleaseIndexItem>> GetActiveMajorReleaseIndexItemsAsync()
    {
        var index = _majorReleasesIndex ?? await GetMajorReleasesIndexAsync();
        return index.ReleasesIndex.Where(item => item.SupportPhase is not (SupportPhase.Eol or SupportPhase.Preview));
    }

    public async Task<string> GetLatestPatchReleaseVersionAsync(string majorVersion)
    {
        var index = _majorReleasesIndex ?? await GetMajorReleasesIndexAsync();
        return index.ReleasesIndex.FirstOrDefault(item => item.ChannelVersion == majorVersion)?.LatestRelease ?? string.Empty;
    }

    public async Task<List<string>> GetPatchReleasesSinceVersionAsync(string majorVersion, string sinceVersion)
    {
        var index = _majorReleasesIndex ?? await GetMajorReleasesIndexAsync();
        var releasesUri = index.ReleasesIndex
            .FirstOrDefault(i => i.ChannelVersion == majorVersion)
            ?.ReleasesJson;

        if (string.IsNullOrEmpty(releasesUri))
            return new List<string>();

        var stream = await _httpClient.GetStreamAsync(releasesUri);
        var majorRelease = await ReleaseNotes.GetMajorRelease(stream)
            ?? throw new InvalidOperationException("Failed to retrieve patch releases.");

        // try parse once
        Version? sinceVer = Version.TryParse(sinceVersion, out var tmp) ? tmp : null;

        return majorRelease.Releases
            .Where(r =>
            {
                // if both sides parse as a Version, compare numerically
                if (Version.TryParse(r.ReleaseVersion, out var rv) && sinceVer is not null)
                    return rv.CompareTo(sinceVer) > 0;

                // otherwise fallback to ordinal string compare
                return string.Compare(
                    r.ReleaseVersion,
                    sinceVersion,
                    StringComparison.Ordinal) > 0;
            })
            .Select(r => r.ReleaseVersion)
            .ToList();
    }

    public async Task<List<string>> GetCvesSinceVersionAsync(string majorVersion, string sinceVersion)
    {
        var patchVersions = await GetPatchReleasesSinceVersionAsync(majorVersion, sinceVersion);

        var index = _majorReleasesIndex ?? await GetMajorReleasesIndexAsync();
        var releaseUri = index.ReleasesIndex
            .FirstOrDefault(i => i.ChannelVersion == majorVersion)
            ?.ReleasesJson;
        if (string.IsNullOrEmpty(releaseUri))
            return new List<string>();

        var majorRelease = await ReleaseNotes.GetMajorRelease(
            await _httpClient.GetStreamAsync(releaseUri))
            ?? throw new InvalidOperationException("Failed to retrieve major release.");

        // flatten all CVE entries for matching patches, select their CveId
        return majorRelease.Releases
            .Where(r => patchVersions.Contains(r.ReleaseVersion))
            .SelectMany(r => r.CveList)      // get every affected CVE
            .Select(c => c.CveId)            // pull out the ID
            .Distinct()                      // optional: remove duplicates
            .ToList();
    }

    public async Task<string> GetInstallerUrlForVersion(string major)
    {
        var index = _majorReleasesIndex ?? await GetMajorReleasesIndexAsync();
        var release = index.ReleasesIndex.FirstOrDefault(i => i.LatestRelease == major);
        var releaseUri = index.ReleasesIndex
            .FirstOrDefault(i => i.ChannelVersion == major)
            ?.ReleasesJson;

        if (releaseUri == null)
        {
            throw new InvalidOperationException($"No release found for version {major}.");
        }

        var majorRelease = await ReleaseNotes.GetMajorRelease(
            await _httpClient.GetStreamAsync(releaseUri))
            ?? throw new InvalidOperationException("Failed to retrieve major release.");

        string os = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "win",
            PlatformID.Unix => "linux",
            PlatformID.MacOSX => "osx",
            _ => throw new NotSupportedException("Unsupported OS platform.")
        };

        string architecture = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();

        string rid = $"{os}-{architecture}";

        var installer = majorRelease.Releases[0].Runtime.Files
        .Where(f => f.Rid == rid)
        .FirstOrDefault();

        return installer?.Url ?? throw new InvalidOperationException("Installer URI is not available.");
    }

}
