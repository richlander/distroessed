using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonConverter(typeof(KebabCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The support phases of a .NET release through its lifecycle")]
public enum SupportPhase
{
    [Description("Pre-release phase with previews and release candidates")]
    Preview,
    [Description("Production-ready but with limited support")]
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

[Description("Lifecycle information for a .NET release")]
public record Lifecycle(
    [Description("Support model (LTS or STS)")]
    ReleaseType ReleaseType,
    [Description("Current lifecycle phase")]
    SupportPhase phase,
    [Description("General Availability date when the release became stable")]
    DateTimeOffset GaDate,
    [Description("End of Life date when support ends")]
    DateTimeOffset EolDate)
{
    [Description("Whether this release is currently supported (based on EOL date and lifecycle phase)")]
    public bool Supported { get; set; } = false;
};

public enum ProductComponent
{
    Runtime,
    SDK
}

public static class ReleaseStability
{
    /// <summary>
    /// Determines if a release is stable (suitable for latest/latest-lts links).
    /// Stable releases are those in Active, Maintenance, or GoLive phases.
    /// </summary>
    /// <param name="phase">The support phase to check</param>
    /// <returns>True if the release is stable, false otherwise</returns>
    public static bool IsStable(SupportPhase phase)
    {
        return phase switch
        {
            SupportPhase.Active => true,
            SupportPhase.Maintenance => true,
            SupportPhase.GoLive => true,
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
        return lifecycle != null && IsStable(lifecycle.phase);
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
        // 1. It's in a stable phase (Active, Maintenance, or GoLive)
        // 2. It hasn't reached its EOL date
        return IsStable(lifecycle.phase) && checkDate < lifecycle.EolDate;
    }
}
