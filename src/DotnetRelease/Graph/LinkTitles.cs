namespace DotnetRelease.Graph;

/// <summary>
/// Standard link titles for .NET release index files
/// Uses string interning to ensure single instances of commonly used strings
/// </summary>
public static class LinkTitles
{
    // Common link titles
    public static readonly string Index = string.Intern("Index");
    public static readonly string HistoryIndex = string.Intern("History Index");
    public static readonly string DotNetReleaseIndex = string.Intern(".NET Release Index");
    public static readonly string DotNetReleaseNotes = string.Intern(".NET Release Notes");
    public static readonly string ReleaseNotes = string.Intern("Release Notes");
    public static readonly string Release = string.Intern("Release");
    public static readonly string ReleaseManifest = string.Intern("Release manifest");
    public static readonly string CompleteReleaseInformation = string.Intern("Complete (large file) release information for all patch releases");
    
    // CVE-related
    public static readonly string CveInformation = string.Intern("CVE Information");
    
    // Support and documentation
    public static readonly string SupportPolicy = string.Intern("Support Policy");
    public static readonly string UsageGuide = string.Intern("Usage Guide");
    public static readonly string QuickReference = string.Intern("Quick Reference");
    public static readonly string Glossary = string.Intern("Glossary");
    
    // OS and packages
    public static readonly string SupportedOSes = string.Intern("Supported OSes");
    public static readonly string LinuxPackages = string.Intern("Linux Packages");
}
