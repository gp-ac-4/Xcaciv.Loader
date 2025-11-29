using System;
using System.Collections.Generic;

namespace Xcaciv.Loader;

/// <summary>
/// Defines security policies for assembly loading operations.
/// Controls which directories are forbidden and the strictness of security checks.
/// </summary>
/// <remarks>
/// <para>This class provides pre-configured security policies as well as the ability to create custom policies.</para>
/// <para><strong>Pre-configured Policies:</strong></para>
/// <list type="bullet">
///   <item><description><see cref="Default"/>: Basic protection against loading from sensitive system directories</description></item>
///   <item><description><see cref="Strict"/>: Enhanced protection blocking additional system and credential directories</description></item>
/// </list>
/// </remarks>
public class AssemblySecurityPolicy
{
    // Default list of forbidden directories
    private static readonly string[] DefaultForbiddenDirectories =
    [
        "grouppolicy",
        "systemprofile"
    ];
    
    // Extended list of forbidden directories for strict mode
    private static readonly string[] StrictForbiddenDirectories =
    [
        // Basic system directories
        "windows", "system32", "programfiles", "programfiles(x86)", "programdata",
        // Specific sensitive directories
        "grouppolicy", "systemprofile", "winevt\\logs", "credentials", 
        "windows defender", "appdata\\local\\microsoft\\credentials"
    ];
    
    /// <summary>
    /// Gets the default security policy with basic system directory restrictions.
    /// </summary>
    /// <remarks>
    /// This policy blocks loading from:
    /// <list type="bullet">
    ///   <item><description>Group Policy directories</description></item>
    ///   <item><description>System Profile directories</description></item>
    /// </list>
    /// <para>Suitable for most production scenarios.</para>
    /// </remarks>
    public static AssemblySecurityPolicy Default { get; } = new();
    
    /// <summary>
    /// Gets the strict security policy with enhanced system directory restrictions.
    /// </summary>
    /// <remarks>
    /// This policy blocks loading from all default directories plus:
    /// <list type="bullet">
    ///   <item><description>Windows system directories (System32, Program Files, etc.)</description></item>
    ///   <item><description>Credential and password stores</description></item>
    ///   <item><description>Windows Defender directories</description></item>
    ///   <item><description>Event log directories</description></item>
    /// </list>
    /// <para>Recommended for high-security environments.</para>
    /// </remarks>
    public static AssemblySecurityPolicy Strict { get; } = new(strictMode: true);
    
    /// <summary>
    /// Gets a value indicating whether strict mode is enabled.
    /// </summary>
    public bool StrictMode { get; init; }
    
    /// <summary>
    /// Gets the read-only list of forbidden directory names (case-insensitive).
    /// </summary>
    /// <remarks>
    /// Directory names are matched as substrings within the full path.
    /// For example, "system32" will match "C:\Windows\System32\".
    /// </remarks>
    public IReadOnlyList<string> ForbiddenDirectories { get; init; }
    
    /// <summary>
    /// Creates a new security policy with the specified configuration.
    /// </summary>
    /// <param name="strictMode">
    /// If true, uses the extended list of forbidden directories.
    /// If false (default), uses the basic list.
    /// </param>
    /// <example>
    /// <code>
    /// // Create a default policy
    /// var policy = new AssemblySecurityPolicy();
    /// 
    /// // Create a strict policy
    /// var strictPolicy = new AssemblySecurityPolicy(strictMode: true);
    /// 
    /// // Or use the pre-configured static instances
    /// var defaultPolicy = AssemblySecurityPolicy.Default;
    /// var strictPolicy = AssemblySecurityPolicy.Strict;
    /// </code>
    /// </example>
    public AssemblySecurityPolicy(bool strictMode = false)
    {
        StrictMode = strictMode;
        ForbiddenDirectories = strictMode 
            ? StrictForbiddenDirectories 
            : DefaultForbiddenDirectories;
    }
    
    /// <summary>
    /// Creates a custom security policy with a specific list of forbidden directories.
    /// </summary>
    /// <param name="forbiddenDirectories">The list of directory names to forbid (case-insensitive)</param>
    /// <exception cref="ArgumentNullException">Thrown when forbiddenDirectories is null</exception>
    /// <example>
    /// <code>
    /// // Create a custom policy blocking specific directories
    /// var customPolicy = new AssemblySecurityPolicy(
    ///     forbiddenDirectories: new[] { "temp", "downloads", "desktop" });
    /// 
    /// var context = new AssemblyContext(
    ///     dllPath, 
    ///     basePathRestriction: basePath,
    ///     securityPolicy: customPolicy);
    /// </code>
    /// </example>
    public AssemblySecurityPolicy(string[] forbiddenDirectories)
    {
        ArgumentNullException.ThrowIfNull(forbiddenDirectories, nameof(forbiddenDirectories));
        
        StrictMode = false; // Custom policy
        ForbiddenDirectories = forbiddenDirectories;
    }
    
    /// <summary>
    /// Checks if a given path contains any forbidden directory.
    /// </summary>
    /// <param name="fullPath">The full path to check (case-insensitive)</param>
    /// <returns>True if the path contains a forbidden directory, false otherwise</returns>
    /// <remarks>
    /// This method performs a case-insensitive substring match against the forbidden directories.
    /// </remarks>
    public bool ContainsForbiddenDirectory(string fullPath)
    {
        if (String.IsNullOrWhiteSpace(fullPath))
            return false;
        
        var lowerPath = fullPath.ToLowerInvariant();
        foreach (var forbiddenDir in ForbiddenDirectories)
        {
            if (lowerPath.Contains($"\\{forbiddenDir}\\", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        
        return false;
    }
}
