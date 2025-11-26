using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The support phases of a .NET release through its lifecycle")]
public enum SupportPhase
{
    [Description("Pre-release phase with preview releases")]
    Preview,
    [Description("Pre-release phase with supported release candidate releases")]
    GoLive,
    [Description("Full support with regular updates and security fixes")]
    Active,
    [Description("Security updates only, no new features")]
    Maintenance,
    [Description("End of life, no further updates")]
    Eol
}

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<ReleaseType>))]
[Description("The release support models offering different support lengths")]
public enum ReleaseType
{
    [Description("Long Term Support - 3 years of support")]
    LTS,
    [Description("Standard Term Support - 18 months of support")]
    STS,
}

[Description("Lifecycle information for a .NET major release")]
public record Lifecycle(
    [property:JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support model (LTS or STS), null for feature bands")]
    ReleaseType? ReleaseType,
    [property: Description("Current lifecycle phase")]
    SupportPhase Phase,
    [property: JsonPropertyName("ga_date"),
     Description("Date when the version became generally available")]
    DateTimeOffset GaDate,
    [property: Description("End of Life date when support ends")]
    DateTimeOffset EolDate)
{
    [property: Description("Whether this release is currently supported (based on EOL date and lifecycle phase)")]
    public bool Supported { get; set; } = false;
};

[Description("Simplified lifecycle information for a .NET patch release")]
public record PatchLifecycle(
    [property: Description("Current lifecycle phase")]
    SupportPhase Phase,
    [property: JsonPropertyName("ga_date"),
     Description("Date when the patch version became generally available")]
    DateTimeOffset GaDate);

public enum ProductComponent
{
    Runtime,
    SDK
}

public static class ReleaseStability
{
    /// <summary>
    /// Determines if a release is stable (suitable for latest/latest-lts links).
    /// Stable releases are those in Active or Maintenance phases.
    /// </summary>
    /// <param name="phase">The support phase to check</param>
    /// <returns>True if the release is stable, false otherwise</returns>
    public static bool IsStable(SupportPhase phase)
    {
        return phase switch
        {
            SupportPhase.Active => true,
            SupportPhase.Maintenance => true,
            SupportPhase.Preview => false,
            SupportPhase.Eol => false,
            _ => false
        };
    }

    /// <summary>
    /// Determines if a lifecycle is stable (suitable for latest/latest-lts links).
    /// </summary>
    /// <param name="lifecycle">The lifecycle to check</param>
    /// <returns>True if the lifecycle is stable, false otherwise</returns>
    public static bool IsStable(Lifecycle? lifecycle)
    {
        return lifecycle != null && IsStable(lifecycle.Phase);
    }

    /// <summary>
    /// Determines if a release is currently supported based on its lifecycle phase and EOL date.
    /// </summary>
    /// <param name="lifecycle">The lifecycle to check</param>
    /// <param name="referenceDate">The date to check against (typically DateTime.UtcNow)</param>
    /// <returns>True if the release is currently supported, false otherwise</returns>
    public static bool IsSupported(Lifecycle? lifecycle, DateTimeOffset? referenceDate = null)
    {
        if (lifecycle == null)
            return false;

        var checkDate = referenceDate ?? DateTimeOffset.UtcNow;

        // A release is supported if:
        // 1. It's in a stable phase (Active or Maintenance)
        // 2. It hasn't reached its EOL date
        return IsStable(lifecycle.Phase) && checkDate < lifecycle.EolDate;
    }

    /// <summary>
    /// Finds the latest stable release version from a collection.
    /// Latest is defined as the highest version number among stable (Active or Maintenance) releases.
    /// </summary>
    /// <param name="releases">Collection of releases to search</param>
    /// <param name="comparer">String comparer for version ordering (should use numeric ordering)</param>
    /// <returns>The latest stable release version, or null if none found</returns>
    public static string? FindLatestVersion(
        IEnumerable<(string Version, Lifecycle? Lifecycle)> releases,
        StringComparer comparer)
    {
        return releases
            .Where(r => IsStable(r.Lifecycle))
            .OrderByDescending(r => r.Version, comparer)
            .Select(r => r.Version)
            .FirstOrDefault();
    }

    /// <summary>
    /// Finds the latest stable LTS release version from a collection.
    /// Latest LTS is defined as the highest version number among stable LTS releases.
    /// </summary>
    /// <param name="releases">Collection of releases to search</param>
    /// <param name="comparer">String comparer for version ordering (should use numeric ordering)</param>
    /// <returns>The latest stable LTS release version, or null if none found</returns>
    public static string? FindLatestLtsVersion(
        IEnumerable<(string Version, Lifecycle? Lifecycle)> releases,
        StringComparer comparer)
    {
        return releases
            .Where(r => r.Lifecycle != null &&
                       IsStable(r.Lifecycle) &&
                       r.Lifecycle.ReleaseType == ReleaseType.LTS)
            .OrderByDescending(r => r.Version, comparer)
            .Select(r => r.Version)
            .FirstOrDefault();
    }

    /// <summary>
    /// Computes the effective support phase based on the specified phase and GA date.
    /// If the phase is Preview or GoLive and the GA date has passed, returns Active.
    /// This ensures consistency between tools that read different source data.
    /// </summary>
    /// <param name="phase">The specified support phase (from source data)</param>
    /// <param name="gaDate">The GA (general availability) date</param>
    /// <param name="referenceDate">The date to check against (defaults to now)</param>
    /// <returns>The effective support phase</returns>
    public static SupportPhase ComputeEffectivePhase(SupportPhase phase, DateTimeOffset gaDate, DateTimeOffset? referenceDate = null)
    {
        var checkDate = referenceDate ?? DateTimeOffset.UtcNow;

        // If the phase is Preview/GoLive but the GA date has passed, transition to Active
        if ((phase == SupportPhase.Preview || phase == SupportPhase.GoLive) && gaDate <= checkDate)
        {
            return SupportPhase.Active;
        }

        // If the phase is Active but the GA date is in the future, it should be Preview
        if (phase == SupportPhase.Active && gaDate > checkDate)
        {
            return SupportPhase.Preview;
        }

        return phase;
    }

    /// <summary>
    /// Determines the support phase based on a patch version string.
    /// This is used for historical timeline indexes where the phase should reflect
    /// what the release was at that point in time, not the current state.
    /// </summary>
    /// <param name="patchVersion">The patch version string (e.g., "10.0.0", "10.0.0-preview.5", "10.0.0-rc.1")</param>
    /// <returns>The support phase based on the version string</returns>
    /// <remarks>
    /// Rules:
    /// - Versions containing "preview" → Preview
    /// - Versions containing "rc" → GoLive (release candidate, supported)
    /// - Versions ending in ".0.0" or higher (no preview/rc suffix) → Active
    /// </remarks>
    public static SupportPhase DeterminePhaseFromVersion(string patchVersion)
    {
        if (string.IsNullOrEmpty(patchVersion))
        {
            return SupportPhase.Preview;
        }

        var lowerVersion = patchVersion.ToLowerInvariant();

        // Check for preview versions
        if (lowerVersion.Contains("preview"))
        {
            return SupportPhase.Preview;
        }

        // Check for release candidate versions (go-live supported)
        if (lowerVersion.Contains("-rc"))
        {
            return SupportPhase.GoLive;
        }

        // GA releases (no preview/rc suffix) are active
        // This includes ".0.0" and all subsequent patches like ".0.1", ".1", etc.
        return SupportPhase.Active;
    }
}
