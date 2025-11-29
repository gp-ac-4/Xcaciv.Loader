using System;
using System.IO;

namespace Xcaciv.Loader;

/// <summary>
/// Utility methods for validating and sanitizing assembly paths before use with AssemblyContext.
/// Provides canonical input handling to prevent common security vulnerabilities.
/// </summary>
/// <remarks>
/// <para>Use these methods to validate user-provided paths before passing them to <see cref="AssemblyContext"/>.</para>
/// <para>These utilities implement defense-in-depth by providing an additional validation layer.</para>
/// </remarks>
public static class AssemblyPathValidator
{
    /// <summary>
    /// Sanitizes an assembly path by removing potentially dangerous characters and normalizing separators.
    /// This method provides canonical input handling to prevent path-based attacks.
    /// </summary>
    /// <param name="path">The path to sanitize</param>
    /// <returns>A sanitized version of the path with normalized separators and removed dangerous characters</returns>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, or whitespace</exception>
    /// <remarks>
    /// <para>This method performs the following operations:</para>
    /// <list type="bullet">
    ///   <item><description>Removes null bytes (path traversal attack vector)</description></item>
    ///   <item><description>Normalizes path separators to the platform-specific separator</description></item>
    ///   <item><description>Removes double separators</description></item>
    /// </list>
    /// <para><strong>Security Note:</strong> This is a defense-in-depth measure. Always use with <see cref="AssemblyContext.VerifyPath"/> for complete validation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Sanitize user input before using
    /// string userPath = GetPathFromUser();
    /// string sanitizedPath = AssemblyPathValidator.SanitizeAssemblyPath(userPath);
    /// 
    /// using var context = new AssemblyContext(
    ///     sanitizedPath,
    ///     basePathRestriction: pluginDirectory);
    /// </code>
    /// </example>
    public static string SanitizeAssemblyPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        
        // Remove null bytes (common path traversal attack vector)
        path = path.Replace("\0", String.Empty);
        
        // Normalize path separators to platform-specific separator
        path = path.Replace('/', Path.DirectorySeparatorChar);
        path = path.Replace('\\', Path.DirectorySeparatorChar);
        
        // Remove double separators that could be used to bypass validation
        var doubleSeparator = $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}";
        while (path.Contains(doubleSeparator))
        {
            path = path.Replace(doubleSeparator, Path.DirectorySeparatorChar.ToString());
        }
        
        // Trim any leading or trailing whitespace and separators
        path = path.Trim().Trim(Path.DirectorySeparatorChar);
        
        return path;
    }
    
    /// <summary>
    /// Resolves a relative path to an absolute path relative to the application base directory.
    /// </summary>
    /// <param name="relativePath">The relative path to resolve</param>
    /// <returns>An absolute path resolved relative to the application base directory</returns>
    /// <exception cref="ArgumentException">Thrown when relativePath is null, empty, or whitespace</exception>
    /// <remarks>
    /// <para>This method is useful for resolving plugin paths relative to the application directory.</para>
    /// <para>The returned path is normalized using <see cref="Path.GetFullPath"/>.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Resolve plugin path relative to application directory
    /// string pluginPath = AssemblyPathValidator.ResolveRelativeToBase("Plugins\\MyPlugin.dll");
    /// // Result: C:\MyApp\Plugins\MyPlugin.dll (if app is in C:\MyApp)
    /// 
    /// using var context = new AssemblyContext(
    ///     pluginPath,
    ///     basePathRestriction: Path.GetDirectoryName(pluginPath));
    /// </code>
    /// </example>
    public static string ResolveRelativeToBase(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath, nameof(relativePath));
        
        var basePath = AppDomain.CurrentDomain.BaseDirectory;
        var combinedPath = Path.Combine(basePath, relativePath);
        
        // Normalize the path to handle any .. or . in the path
        return Path.GetFullPath(combinedPath);
    }
    
    /// <summary>
    /// Performs basic heuristic checks to determine if a path appears safe for assembly loading.
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <returns>True if the path passes basic safety checks, false otherwise</returns>
    /// <remarks>
    /// <para>This method performs basic heuristic checks including:</para>
    /// <list type="bullet">
    ///   <item><description>Path is not null, empty, or whitespace</description></item>
    ///   <item><description>Path does not contain null bytes</description></item>
    ///   <item><description>Path does not contain ".." (potential path traversal)</description></item>
    ///   <item><description>Path does not contain wildcard characters</description></item>
    /// </list>
    /// <para><strong>Important:</strong> This is a basic safety check only. Always use <see cref="AssemblyContext.VerifyPath"/> for complete validation.</para>
    /// <para><strong>Note:</strong> A path passing this check does NOT guarantee it is safe - it only filters obvious attack patterns.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string userPath = GetPathFromUser();
    /// 
    /// if (!AssemblyPathValidator.IsSafePath(userPath))
    /// {
    ///     logger.LogWarning("Rejecting potentially unsafe path: {Path}", userPath);
    ///     return;
    /// }
    /// 
    /// // Additional validation will be performed by AssemblyContext.VerifyPath
    /// using var context = new AssemblyContext(userPath, basePathRestriction: pluginDir);
    /// </code>
    /// </example>
    public static bool IsSafePath(string path)
    {
        // Basic validation
        if (String.IsNullOrWhiteSpace(path))
            return false;
        
        // Check for null bytes (path traversal attack vector)
        if (path.Contains('\0'))
            return false;
        
        // Check for path traversal attempts
        if (path.Contains(".."))
            return false;
        
        // Check for wildcard characters that might bypass validation
        if (path.Contains('*') || path.Contains('?'))
            return false;
        
        // Check for potentially dangerous characters
        if (path.Contains('<') || path.Contains('>') || path.Contains('|'))
            return false;
        
        return true;
    }
    
    /// <summary>
    /// Validates that a path has an allowed assembly file extension (.dll or .exe).
    /// </summary>
    /// <param name="path">The path to validate</param>
    /// <returns>True if the path has a .dll or .exe extension (case-insensitive), false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, or whitespace</exception>
    /// <remarks>
    /// <para>This check ensures that only assembly files are loaded, preventing attempts to load other file types.</para>
    /// <para><strong>Note:</strong> This is a preliminary check. <see cref="AssemblyContext.VerifyPath"/> performs additional validation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// string filePath = @"C:\Plugins\MyPlugin.dll";
    /// if (AssemblyPathValidator.HasValidAssemblyExtension(filePath))
    /// {
    ///     using var context = new AssemblyContext(filePath, basePathRestriction: @"C:\Plugins");
    /// }
    /// </code>
    /// </example>
    public static bool HasValidAssemblyExtension(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        
        var extension = Path.GetExtension(path);
        
        return extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".exe", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Combines path validation, sanitization, and resolution into a single operation.
    /// This is the recommended method for processing user-provided assembly paths.
    /// </summary>
    /// <param name="path">The path to validate and sanitize</param>
    /// <param name="resolveRelativeToBase">If true, relative paths are resolved relative to the application base directory</param>
    /// <returns>A validated and sanitized absolute path ready for use with AssemblyContext</returns>
    /// <exception cref="ArgumentException">Thrown when path is null, empty, whitespace, or fails safety checks</exception>
    /// <remarks>
    /// <para>This method combines multiple validation steps:</para>
    /// <list type="number">
    ///   <item><description>Basic safety heuristics (<see cref="IsSafePath"/>)</description></item>
    ///   <item><description>Extension validation (<see cref="HasValidAssemblyExtension"/>)</description></item>
    ///   <item><description>Path sanitization (<see cref="SanitizeAssemblyPath"/>)</description></item>
    ///   <item><description>Optional resolution to absolute path (<see cref="ResolveRelativeToBase"/>)</description></item>
    /// </list>
    /// <para><strong>Best Practice:</strong> Use this method for all user-provided paths before passing to <see cref="AssemblyContext"/>.</para>
    /// <para><strong>Note:</strong> <see cref="AssemblyContext.VerifyPath"/> will perform additional validation.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Validate and sanitize user input
    /// string userPath = GetPathFromUser();
    /// 
    /// try
    /// {
    ///     string validatedPath = AssemblyPathValidator.ValidateAndSanitize(
    ///         userPath,
    ///         resolveRelativeToBase: true);
    ///     
    ///     using var context = new AssemblyContext(
    ///         validatedPath,
    ///         basePathRestriction: Path.GetDirectoryName(validatedPath));
    ///     
    ///     var plugin = context.CreateInstance&lt;IPlugin&gt;("MyPlugin");
    /// }
    /// catch (ArgumentException ex)
    /// {
    ///     logger.LogError(ex, "Invalid assembly path provided: {Path}", userPath);
    /// }
    /// </code>
    /// </example>
    public static string ValidateAndSanitize(string path, bool resolveRelativeToBase = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));
        
        // Step 1: Basic safety checks
        if (!IsSafePath(path))
        {
            throw new ArgumentException(
                $"Path failed safety checks. The path contains potentially dangerous patterns: {path}",
                nameof(path));
        }
        
        // Step 2: Sanitize the path
        var sanitizedPath = SanitizeAssemblyPath(path);
        
        // Step 3: Resolve to absolute path if requested
        if (resolveRelativeToBase && !Path.IsPathRooted(sanitizedPath))
        {
            sanitizedPath = ResolveRelativeToBase(sanitizedPath);
        }
        
        // Step 4: Validate extension
        if (!HasValidAssemblyExtension(sanitizedPath))
        {
            throw new ArgumentException(
                $"Path does not have a valid assembly extension (.dll or .exe): {sanitizedPath}",
                nameof(path));
        }
        
        return sanitizedPath;
    }
}
