namespace DotnetRelease;

/// <summary>
/// Represents a single taxonomy item with identifier, display name, and description.
/// </summary>
public record TaxonomyItem(
    string String,
    string DisplayName,
    string Description
);

/// <summary>
/// Represents a taxonomy category with its items.
/// </summary>
public record TaxonomyCategory(
    string Category,
    string Description,
    IReadOnlyList<TaxonomyItem> Items
);

/// <summary>
/// Maps CVE taxonomy values to their display names.
/// Based on taxonomy files in /home/rich/git/designs/accepted/2025/cve-schema/
/// This class provides both lookup functionality and the ability to generate taxonomy JSON files.
/// </summary>
public static class TaxonomyNames
{
    #region Products
    private static readonly TaxonomyItem[] _products =
    [
        new("dotnet-runtime", ".NET Runtime Libraries", "The .NET runtime libraries, such as CoreCLR and System.Text.Json"),
        new("dotnet-aspnetcore", "ASP.NET Core Runtime", "The ASP.NET Core runtime, such as Kestrel and ASP.NET Core MVC"),
        new("dotnet-windows-desktop", ".NET Windows Desktop Runtime", "Windows-specific libraries, such as Windows Forms, WPF, and WinRT bindings"),
        new("dotnet-sdk", ".NET SDK", "The primary developer component, including the CLI, MSBuild, and all of the runtime libraries")
    ];

    private static readonly Dictionary<string, string> _productDisplayNames = _products.ToDictionary(
        p => p.String,
        p => p.DisplayName,
        StringComparer.OrdinalIgnoreCase);

    public static string GetProductDisplayName(string productId) =>
        _productDisplayNames.TryGetValue(productId, out var displayName) ? displayName : productId;

    public static TaxonomyCategory GetProductsTaxonomy() =>
        new("products", "Official .NET product identifiers and their display information", _products);
    #endregion

    #region Platforms
    private static readonly TaxonomyItem[] _platforms =
    [
        new("linux", "Linux", "Linux distros, such as Alpine, Red Hat, and Ubuntu"),
        new("macos", "macOS", "Apple macOS"),
        new("windows", "Windows", "Microsoft Windows"),
        new("all", "All Platforms", "All operating systems or not specific to OS-specific libraries")
    ];

    private static readonly Dictionary<string, string> _platformDisplayNames = _platforms.ToDictionary(
        p => p.String,
        p => p.DisplayName,
        StringComparer.OrdinalIgnoreCase);

    public static string GetPlatformDisplayName(string platformId) =>
        _platformDisplayNames.TryGetValue(platformId, out var displayName) ? displayName : platformId;

    public static TaxonomyCategory GetPlatformsTaxonomy() =>
        new("platforms", "Supported operating system platforms for .NET vulnerabilities", _platforms);
    #endregion

    #region Architectures
    private static readonly TaxonomyItem[] _architectures =
    [
        new("arm", "Arm32", "Arm32 (32-bit; AKA armhf)"),
        new("arm64", "Arm64", "Arm64 (64-bit)"),
        new("x64", "x64", "x86-x64 (64-bit)"),
        new("x86", "x86", "x86 (32-bit)"),
        new("all", "All Architectures", "All architectures or not specific to architecture-specific libraries")
    ];

    private static readonly Dictionary<string, string> _architectureDisplayNames = _architectures.ToDictionary(
        p => p.String,
        p => p.DisplayName,
        StringComparer.OrdinalIgnoreCase);

    public static string GetArchitectureDisplayName(string architectureId) =>
        _architectureDisplayNames.TryGetValue(architectureId, out var displayName) ? displayName : architectureId;

    public static TaxonomyCategory GetArchitecturesTaxonomy() =>
        new("architectures", "Supported processor architectures for .NET vulnerabilities", _architectures);
    #endregion

    #region Severity
    private static readonly TaxonomyItem[] _severities =
    [
        new("critical", "Critical", "Immediate action required - exploitable vulnerabilities with severe impact"),
        new("high", "High", "High priority - significant security risk requiring prompt attention"),
        new("medium", "Medium", "Moderate risk - should be addressed in regular maintenance cycles"),
        new("low", "Low", "Low risk - minimal impact vulnerabilities")
    ];

    private static readonly Dictionary<string, string> _severityDisplayNames = _severities.ToDictionary(
        p => p.String,
        p => p.DisplayName,
        StringComparer.OrdinalIgnoreCase);

    public static string GetSeverityDisplayName(string severityId) =>
        _severityDisplayNames.TryGetValue(severityId, out var displayName) ? displayName : severityId;

    public static TaxonomyCategory GetSeverityTaxonomy() =>
        new("severity", "CVE severity levels based on CVSS scores and impact assessment", _severities);
    #endregion

    #region CNAs
    private static readonly TaxonomyItem[] _cnas =
    [
        new("microsoft", "Microsoft Corporation", "The CNA for .NET CVEs published by Microsoft")
    ];

    private static readonly Dictionary<string, string> _cnaDisplayNames = _cnas.ToDictionary(
        p => p.String,
        p => p.DisplayName,
        StringComparer.OrdinalIgnoreCase);

    public static string GetCnaDisplayName(string cnaId) =>
        _cnaDisplayNames.TryGetValue(cnaId, out var displayName) ? displayName : cnaId;

    public static TaxonomyCategory GetCnasTaxonomy() =>
        new("cnas", "CVE Numbering Authorities that issue .NET CVEs", _cnas);
    #endregion
}

