using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease.Graph;

/// <summary>
/// Target frameworks document for a major .NET version.
/// Lists all supported TFMs including base and platform-specific variants.
/// This is a simple JSON format (not HAL) for easy consumption in other workflows.
/// </summary>
[Description("Target frameworks supported by a major .NET version")]
public record TargetFrameworksIndex(
    [property: Description("Major version identifier (e.g., '10.0')")]
    string Version,
    [property: Description("Human-friendly name (e.g., '.NET 10', '.NET Core 3.0')")]
    string Name,
    [property: Description("Base target framework moniker (e.g., 'net10.0')")]
    string TargetFramework)
{
    [Description("List of supported target frameworks")]
    public IReadOnlyList<TargetFrameworkEntry> Frameworks { get; init; } = [];
}

/// <summary>
/// A single target framework entry within a target frameworks document.
/// </summary>
[Description("Target framework entry with platform-specific details")]
public record TargetFrameworkEntry(
    [property: Description("Target framework moniker (e.g., 'net10.0', 'net10.0-ios')")]
    string Tfm)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Canonical TFM with explicit platform version (e.g., 'net10.0-ios18.7')")]
    public string? Canonical { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Platform identifier (e.g., 'ios', 'android', 'windows')")]
    public string? Platform { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Default platform version for this .NET release (e.g., '18.7' for iOS in .NET 10)")]
    public string? PlatformVersion { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Human-readable description of the target framework")]
    public string? Description { get; init; }
}
