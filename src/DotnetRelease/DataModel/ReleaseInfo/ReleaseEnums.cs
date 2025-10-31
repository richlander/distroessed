using System.ComponentModel;
using System.Text.Json.Serialization;

namespace DotnetRelease;

[JsonConverter(typeof(SnakeCaseLowerStringEnumConverter<SupportPhase>))]
[Description("The support phases of a .NET release through its lifecycle")]
public enum SupportPhase
{
    [Description("Pre-release phase with previews and release candidates")]
    Preview,
    [Description("Full support with regular updates and security fixes")]
    Active,
    [Description("Security updates only, no new features")]
    Maintenance,
    [Description("End of life, no further updates")]
    Eol
}

[JsonConverter(typeof(SnakeCaseLowerStringEnumConverter<ReleaseType>))]
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
    [property: JsonPropertyName("release-type"),
     JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull),
     Description("Support model (LTS or STS), null for feature bands")]
    ReleaseType? ReleaseType,
    [property: JsonPropertyName("phase"),
     Description("Current lifecycle phase")]
    SupportPhase phase,
    [property: JsonPropertyName("release-date"),
     Description("Release date when the version became generally available")]
    DateTimeOffset ReleaseDate,
    [property: JsonPropertyName("eol-date"),
     Description("End of Life date when support ends")]
    DateTimeOffset EolDate)
{
    [property: JsonPropertyName("supported"),
     Description("Whether this release is currently supported (based on EOL date and lifecycle phase)")]
    public bool Supported { get; set; } = false;
};

[Description("Simplified lifecycle information for a .NET patch release")]
public record PatchLifecycle(
    [property: JsonPropertyName("phase"),
     Description("Current lifecycle phase")]
    SupportPhase Phase,
    [property: JsonPropertyName("release-date"),
     Description("Release date when the patch version became generally available")]
    DateTimeOffset ReleaseDate);

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
        // 1. It's in a stable phase (Active or Maintenance)
        // 2. It hasn't reached its EOL date
        return IsStable(lifecycle.phase) && checkDate < lifecycle.EolDate;
    }
}
