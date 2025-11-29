# SSEM Improvement Checklist for Xcaciv.Loader
**Date Created:** 2025-11-29  
**Project:** Xcaciv.Loader - Dynamic Assembly Loading Library  
**Overall SSEM Score:** 8.0/10 (Good)

---

## Executive Summary

The Xcaciv.Loader library demonstrates **strong alignment** with SSEM (Securable Software Engineering Model) principles, scoring particularly high in **Trustworthiness (9/10)** and **Reliability (8/10)**. The event-based audit trail is exemplary, and input validation is comprehensive.

### SSEM Pillar Scores

| Pillar | Score | Grade | Key Strengths |
|--------|-------|-------|---------------|
| **Maintainability** | 7.0/10 | Good | Clear structure, comprehensive documentation, event transparency |
| **Trustworthiness** | 9.0/10 | ? Excellent | Audit events, security controls, path restrictions |
| **Reliability** | 8.0/10 | Good | Input validation, proper disposal, thread safety |
| **Overall** | **8.0/10** | ? **Good** | Production-ready with improvement opportunities |

---

## Priority 1: Critical Security & Reliability Improvements

### ? REL-001: Fix Silent Failure in LoadFromPath Dependency Resolution
**Status:** ? **COMPLETED**  
**SSEM Pillar:** Reliability (Resilience)  
**Priority:** High  
**Effort:** Low  

**Issue:**  
The `LoadFromPath(AssemblyLoadContext context, string path)` helper method silently returns `null` when a `SecurityException` occurs during dependency resolution. This loses critical error information and may cause the application to fail later with unclear error messages.

**Implementation Summary:**
- Modified `LoadFromPath(AssemblyLoadContext, string)` to re-throw SecurityException after raising event
- Added explicit catch blocks for `FileNotFoundException` and `BadImageFormatException` with event raising
- Eliminates silent failures while maintaining audit trail through events

**Impact:**
- ? Eliminates silent failures
- ? Provides clear error messages to consumers
- ? Maintains transparency through events
- ?? Breaking change: May expose exceptions previously hidden

**Files Modified:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (Lines 235-260)

---

### ? REL-002: Reduce Overly Broad Exception Catching
**Status:** ? **COMPLETED**  
**SSEM Pillar:** Reliability (Resilience)  
**Priority:** High  
**Effort:** Medium  

**Issue:**  
Multiple methods use overly broad exception filtering like `catch (Exception ex) when (ex is not ArgumentNullException)`. This pattern can hide unexpected exceptions and make debugging difficult.

**Implementation Summary:**
- Replaced broad exception catches in all three `CreateInstance` overloads
- Added specific catches for: `MissingMethodException`, `TargetInvocationException`, `MemberAccessException`, `TypeLoadException`
- Each catch provides context-specific error messages
- Preserved re-throwing of expected exception types

**Locations Fixed:**
1. `CreateInstance(string className)` - Lines ~415-450
2. `CreateInstance<T>(string className)` - Lines ~460-515
3. `CreateInstance<T>(Type instanceType)` - Lines ~530-555

**Impact:**
- ? Better error diagnostics
- ? Prevents masking unexpected exceptions
- ? Clearer intent in exception handling
- ? Maintains backward compatibility for expected exception types

**Files Modified:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (Multiple locations)

---

### ? TRUST-001: Add Assembly Signature/Hash Verification (Optional)
**Status:** To Do (Optional Enhancement)  
**SSEM Pillar:** Trustworthiness (Integrity)  
**Priority:** Medium  
**Effort:** High  

**Issue:**  
The library relies solely on file system permissions for assembly integrity. No cryptographic verification is performed to ensure assemblies haven't been tampered with.

**Proposed Solution:**
Add optional hash verification:
```csharp
public class AssemblyContext
{
    public string? ExpectedHash { get; init; }
    public HashAlgorithm HashAlgorithm { get; init; } = HashAlgorithm.SHA256;
    
    private void VerifyAssemblyHash(string filePath)
    {
        if (String.IsNullOrEmpty(ExpectedHash)) return;
        
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = Convert.ToBase64String(sha.ComputeHash(stream));
        
        if (!hash.Equals(ExpectedHash, StringComparison.Ordinal))
        {
            var message = $"Assembly hash mismatch. Expected: {ExpectedHash}, Actual: {hash}";
            SecurityViolation?.Invoke(filePath, message);
            throw new SecurityException(message);
        }
    }
}
```

**Impact:**
- ? Protection against tampered assemblies
- ? Additional integrity layer
- ?? Performance overhead for hash calculation
- ?? Requires consumers to maintain hash database

**Files to Create/Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs`
- `src/Xcaciv.Loader/HashAlgorithm.cs` (new enum)
- Documentation updates

---

## Priority 2: Maintainability & Testability Improvements

### ? MAINT-001: Refactor VerifyPath into Smaller Validation Methods
**Status:** To Do  
**SSEM Pillar:** Maintainability (Analyzability, Testability)  
**Priority:** High  
**Effort:** Medium  

**Issue:**  
The `VerifyPath` method is over 100 lines long and has multiple responsibilities:
1. Input validation
2. Path normalization
3. Extension validation
4. System directory checking
5. Base path restriction enforcement
6. File existence checking

**Current Complexity:**
- Lines: ~103 lines
- Cyclomatic Complexity: ~8
- Responsibilities: 6

**Proposed Refactoring:**

```csharp
public static string VerifyPath(string filePath, string basePathRestriction = "*")
{
    ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
    
    var fullFilePath = NormalizePath(filePath);
    ValidateFileExtension(fullFilePath);
    ValidateNotInForbiddenDirectory(fullFilePath);
    var effectiveBasePath = ResolveBasePathRestriction(fullFilePath, basePathRestriction);
    ValidateWithinBasePath(fullFilePath, effectiveBasePath, basePathRestriction);
    WarnIfFileNotExists(fullFilePath);
    
    return fullFilePath;
}

private static string NormalizePath(string filePath) { /* ... */ }
private static void ValidateFileExtension(string fullFilePath) { /* ... */ }
private static void ValidateNotInForbiddenDirectory(string fullFilePath) { /* ... */ }
private static string ResolveBasePathRestriction(string fullFilePath, string basePathRestriction) { /* ... */ }
private static void ValidateWithinBasePath(string fullFilePath, string effectiveBasePath, string restriction) { /* ... */ }
private static void WarnIfFileNotExists(string fullFilePath) { /* ... */ }
```

**Impact:**
- ? Each method tests one concern
- ? Easier to understand and maintain
- ? Better test coverage
- ? Clearer error messages per validation step

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (Lines ~580-690)

---

### ? MAINT-002: Extract Path Validation to Injectable Interface
**Status:** To Do (Optional Enhancement)  
**SSEM Pillar:** Maintainability (Testability, Modifiability)  
**Priority:** Medium  
**Effort:** High  

**Issue:**  
Path validation is tightly coupled to file system operations, making unit testing difficult. Tests require actual file system setup or integration testing.

**Proposed Solution:**

```csharp
// New interface
public interface IPathValidator
{
    string VerifyPath(string filePath, string basePathRestriction = "*");
    bool IsPathSafe(string path);
}

// Default implementation
public class FileSystemPathValidator : IPathValidator
{
    private readonly string[] forbiddenDirectories;
    
    public FileSystemPathValidator(string[]? forbiddenDirectories = null)
    {
        this.forbiddenDirectories = forbiddenDirectories ?? DefaultForbiddenDirectories;
    }
    
    public string VerifyPath(string filePath, string basePathRestriction = "*")
    {
        // Current VerifyPath logic
    }
    
    public bool IsPathSafe(string path) { /* ... */ }
}

// Modified AssemblyContext
public class AssemblyContext : IAssemblyContext
{
    private readonly IPathValidator pathValidator;
    
    public AssemblyContext(
        string filePath, 
        string? fullName = null, 
        bool isCollectible = true, 
        string basePathRestriction = ".",
        IPathValidator? pathValidator = null)
    {
        this.pathValidator = pathValidator ?? new FileSystemPathValidator();
        // ...
    }
}
```

**Impact:**
- ? Testable with mocks
- ? Allows custom validation logic
- ? Better separation of concerns
- ?? Breaking change to constructors
- ?? Adds complexity

**Files to Create/Modify:**
- `src/Xcaciv.Loader/Validation/IPathValidator.cs` (new)
- `src/Xcaciv.Loader/Validation/FileSystemPathValidator.cs` (new)
- `src/Xcaciv.Loader/AssemblyContext.cs` (constructor changes)

---

### ? MAINT-003: Make Security Configuration Instance-Based
**Status:** To Do  
**SSEM Pillar:** Maintainability (Testability, Modifiability)  
**Priority:** High  
**Effort:** Medium  

**Issue:**  
Static mutable state (`ForbiddenDirectories`, `strictDirectoryRestrictionEnabled`) causes problems:
1. Parallel test execution interference
2. Global state changes affect all instances
3. Cannot have different security policies per context

**Current Implementation:**
```csharp
private static string[] ForbiddenDirectories = DefaultForbiddenDirectories;
private static bool strictDirectoryRestrictionEnabled = false;

public static void SetStrictDirectoryRestriction(bool enable)
{
    strictDirectoryRestrictionEnabled = enable;
    ForbiddenDirectories = enable ? StrictForbiddenDirectories : DefaultForbiddenDirectories;
}
```

**Proposed Solution:**

```csharp
// New configuration class
public class AssemblySecurityPolicy
{
    public static AssemblySecurityPolicy Default { get; } = new();
    public static AssemblySecurityPolicy Strict { get; } = new(strictMode: true);
    
    public bool StrictMode { get; init; }
    public IReadOnlyList<string> ForbiddenDirectories { get; init; }
    
    public AssemblySecurityPolicy(bool strictMode = false)
    {
        StrictMode = strictMode;
        ForbiddenDirectories = strictMode 
            ? StrictForbiddenDirectories 
            : DefaultForbiddenDirectories;
    }
    
    private static readonly string[] DefaultForbiddenDirectories = [...];
    private static readonly string[] StrictForbiddenDirectories = [...];
}

// Modified AssemblyContext
public class AssemblyContext : IAssemblyContext
{
    public AssemblySecurityPolicy SecurityPolicy { get; init; }
    
    public AssemblyContext(
        string filePath,
        string? fullName = null,
        bool isCollectible = true,
        string basePathRestriction = ".",
        AssemblySecurityPolicy? securityPolicy = null)
    {
        SecurityPolicy = securityPolicy ?? AssemblySecurityPolicy.Default;
        // Use SecurityPolicy.ForbiddenDirectories instead of static field
    }
}
```

**Impact:**
- ? Eliminates static mutable state
- ? Allows per-instance security policies
- ? Parallel test execution safe
- ? More flexible configuration
- ?? Breaking change (static methods deprecated)
- ?? Migration guide needed for existing consumers

**Files to Create/Modify:**
- `src/Xcaciv.Loader/AssemblySecurityPolicy.cs` (new)
- `src/Xcaciv.Loader/AssemblyContext.cs` (remove static state)
- `src/Xcaciv.Loader/readme.md` (update examples)

---

### ? MAINT-004: Extract GetLoadedTypes to Separate Utility Class
**Status:** To Do  
**SSEM Pillar:** Maintainability (Analyzability)  
**Priority:** Low  
**Effort:** Low  

**Issue:**  
The static method `GetLoadedTypes<T>()` operates on `AppDomain.CurrentDomain`, not on the `AssemblyContext` instance. It doesn't belong in this class.

**Current Code:**
```csharp
public static IEnumerable<Type> GetLoadedTypes<T>()
{
    return AppDomain.CurrentDomain.GetAssemblies()
        .SelectMany(s => s.GetTypes())
        .Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract) ?? [];
}
```

**Proposed Solution:**

```csharp
// New utility class
namespace Xcaciv.Loader;

/// <summary>
/// Utility for scanning and discovering types across loaded assemblies
/// </summary>
public static class AssemblyScanner
{
    /// <summary>
    /// Gets all loaded types in the current AppDomain that implement or extend type T
    /// </summary>
    public static IEnumerable<Type> GetLoadedTypes<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract);
    }
    
    /// <summary>
    /// Gets types from a specific assembly that implement or extend type T
    /// </summary>
    public static IEnumerable<Type> GetTypes<T>(Assembly assembly)
    {
        return assembly.GetTypes()
            .Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract);
    }
}
```

**Impact:**
- ? Better organization
- ? Clear separation of concerns
- ? Can add more scanning utilities
- ?? Breaking change (method moved)

**Files to Create/Modify:**
- `src/Xcaciv.Loader/AssemblyScanner.cs` (new)
- `src/Xcaciv.Loader/AssemblyContext.cs` (remove method or mark obsolete)

---

## Priority 3: Documentation & API Improvements

### ? DOC-001: Enhance XML Documentation on Events
**Status:** To Do  
**SSEM Pillar:** Maintainability (Analyzability), Trustworthiness (Accountability)  
**Priority:** Medium  
**Effort:** Low  

**Issue:**  
Event documentation lacks:
- Usage examples
- Thread-safety guarantees
- Timing information (when events fire)
- Exception handling guidance

**Current Example:**
```csharp
/// <summary>
/// Raised when an assembly is successfully loaded. Provides audit trail of assembly loading operations.
/// Parameters: (filePath, assemblyName, version)
/// </summary>
public event Action<string, string, Version?>? AssemblyLoaded;
```

**Proposed Enhancement:**
```csharp
/// <summary>
/// Raised when an assembly is successfully loaded into the context.
/// Provides an audit trail of assembly loading operations for security and diagnostic purposes.
/// </summary>
/// <remarks>
/// <para><strong>Thread Safety:</strong> This event may be raised from any thread. Handlers must be thread-safe.</para>
/// <para><strong>Timing:</strong> Raised immediately after successful LoadFromAssemblyPath/LoadFromAssemblyName.</para>
/// <para><strong>Parameters:</strong></para>
/// <list type="bullet">
///   <item><description>filePath: Full path to the loaded assembly file</description></item>
///   <item><description>assemblyName: Full name including version and culture (e.g., "MyAssembly, Version=1.0.0.0")</description></item>
///   <item><description>version: Assembly version, or null if version info unavailable</description></item>
/// </list>
/// <para><strong>Example:</strong></para>
/// <code>
/// context.AssemblyLoaded += (path, name, version) =>
/// {
///     logger.LogInformation("Loaded {Name} v{Version} from {Path}", name, version, path);
/// };
/// </code>
/// </remarks>
public event Action<string, string, Version?>? AssemblyLoaded;
```

**Apply to All Events:**
- `AssemblyLoaded`
- `AssemblyLoadFailed`
- `AssemblyUnloaded`
- `SecurityViolation` ? (most important)
- `DependencyResolved`
- `WildcardPathRestrictionUsed` ? (security warning)

**Impact:**
- ? Clearer API contract
- ? Better IntelliSense experience
- ? Fewer support questions
- ? Security best practices documented

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (Lines 70-110)

---

### ? DOC-002: Add Security Guidance to BasePathRestriction
**Status:** To Do  
**SSEM Pillar:** Trustworthiness (Confidentiality), Maintainability (Analyzability)  
**Priority:** High  
**Effort:** Low  

**Issue:**  
The wildcard `"*"` in `basePathRestriction` is dangerous but not prominently warned about in the XML documentation. Current warning is buried in parameter docs.

**Current Documentation:**
```csharp
/// <param name="basePathRestriction">
/// The directory path that the assembly is restricted to being loaded from.
/// WARNING: Use "*" ONLY in controlled test environments. 
/// In production, ALWAYS specify an explicit directory path to prevent arbitrary code execution.
/// </param>
```

**Proposed Enhancement:**

```csharp
/// <summary>
/// Creates a new AssemblyContext for loading and managing a dynamic assembly.
/// </summary>
/// <param name="filePath">Path to the assembly file to load</param>
/// <param name="fullName">Optional assembly name for the load context</param>
/// <param name="isCollectible">Whether the assembly can be unloaded (default: true)</param>
/// <param name="basePathRestriction">
/// <para>The directory path that assemblies are restricted to loading from.</para>
/// <para><strong>?? SECURITY CRITICAL:</strong></para>
/// <list type="bullet">
///   <item><description><strong>Production:</strong> ALWAYS specify an explicit directory path (e.g., @"C:\MyApp\Plugins")</description></item>
///   <item><description><strong>Testing:</strong> Use "*" only in isolated test environments</description></item>
///   <item><description><strong>Default ".":</strong> Restricts to current directory (secure)</description></item>
/// </list>
/// <para>Using "*" allows loading assemblies from ANY location, including system directories,
/// which can lead to arbitrary code execution vulnerabilities.</para>
/// </param>
/// <example>
/// <code>
/// // ? SECURE: Explicit path restriction
/// var context = new AssemblyContext(
///     pluginPath, 
///     basePathRestriction: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"));
/// 
/// // ?? INSECURE: Wildcard (test only)
/// var context = new AssemblyContext(pluginPath, basePathRestriction: "*");
/// </code>
/// </example>
/// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
/// <exception cref="SecurityException">Thrown when path validation fails</exception>
public AssemblyContext(string filePath, string? fullName = null, bool isCollectible = true, string basePathRestriction = ".")
```

**Impact:**
- ? Prominent security warning
- ? Clear examples of correct usage
- ? Reduces security misconfigurations
- ? Better developer experience

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (Constructor documentation)
- `src/Xcaciv.Loader/readme.md` (Add security section if missing)

---

### ? API-001: Add Input Sanitization Helpers
**Status:** To Do (Optional Enhancement)  
**SSEM Pillar:** Reliability (Integrity - Canonical Input Handling)  
**Priority:** Low  
**Effort:** Low  

**Issue:**  
Consumers may pass unsanitized paths to AssemblyContext. Providing helper methods encourages secure practices.

**Proposed API:**

```csharp
namespace Xcaciv.Loader;

/// <summary>
/// Utility methods for safely handling assembly paths
/// </summary>
public static class PathHelpers
{
    /// <summary>
    /// Sanitizes an assembly path by removing potentially dangerous characters and normalizing separators
    /// </summary>
    public static string SanitizeAssemblyPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        
        // Remove null bytes (path traversal attack vector)
        path = path.Replace("\0", String.Empty);
        
        // Normalize path separators
        path = path.Replace('/', Path.DirectorySeparatorChar);
        
        // Remove double separators
        while (path.Contains($"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}"))
        {
            path = path.Replace(
                $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}", 
                Path.DirectorySeparatorChar.ToString());
        }
        
        return path;
    }
    
    /// <summary>
    /// Resolves a path relative to the application base directory
    /// </summary>
    public static string ResolveRelativeToBase(string relativePath)
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
    }
    
    /// <summary>
    /// Checks if a path appears to be safe (basic heuristics)
    /// </summary>
    public static bool IsSafePath(string path)
    {
        if (String.IsNullOrWhiteSpace(path)) return false;
        if (path.Contains("\0")) return false;
        if (path.Contains("..")) return false; // Path traversal attempt
        
        return true;
    }
}
```

**Impact:**
- ? Encourages secure coding practices
- ? Reduces consumer errors
- ? Canonical input handling
- ?? Adds API surface area

**Files to Create:**
- `src/Xcaciv.Loader/PathHelpers.cs` (new)

---

## Priority 4: Performance & Modern .NET Improvements

### ? PERF-001: Make UnloadAsync Truly Asynchronous
**Status:** To Do (Future Enhancement)  
**SSEM Pillar:** Reliability (Availability)  
**Priority:** Low  
**Effort:** Medium  

**Issue:**  
`UnloadAsync` wraps synchronous code in `Task.Run`, which:
1. Doesn't provide true async benefits
2. Uses thread pool resources unnecessarily
3. May mislead consumers about async nature

**Current Implementation:**
```csharp
public async Task<bool> UnloadAsync()
{
    // ...
    return await Task.Run(() =>
    {
        lock (syncLock)
        {
            // Synchronous unload logic
        }
    }, disposalTokenSource.Token);
}
```

**Challenge:**  
.NET's `AssemblyLoadContext.Unload()` is inherently synchronous. True async unloading is not currently supported by the framework.

**Proposed Options:**

**Option A: Document the limitation**
```csharp
/// <summary>
/// Asynchronously unloads the assembly context.
/// </summary>
/// <remarks>
/// <para><strong>Note:</strong> While this method is async, assembly unloading itself is synchronous.
/// This method uses Task.Run to avoid blocking the caller, but the actual unload operation
/// is performed synchronously on a thread pool thread.</para>
/// <para>Use this method when calling from UI threads or other contexts where blocking is undesirable.</para>
/// </remarks>
public async Task<bool> UnloadAsync() { /* ... */ }
```

**Option B: Remove async version**
- Keep only synchronous `Unload()`
- Consumers can wrap in Task.Run if needed
- More honest about capabilities

**Option C: Wait for .NET to add true async unload**
- Keep current implementation
- Update when framework supports it

**Recommended:** Option A (document limitation)

**Impact:**
- ? Honest about capabilities
- ? Educates consumers
- ?? No performance improvement possible

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (documentation only)

---

### ? PERF-002: Add Timeout Support for Assembly Loading
**Status:** To Do (Optional Enhancement)  
**SSEM Pillar:** Reliability (Availability)  
**Priority:** Low  
**Effort:** Medium  

**Issue:**  
Assembly loading can hang indefinitely on problematic assemblies or slow network paths. No timeout mechanism exists.

**Proposed Solution:**

```csharp
public class AssemblyContext : IAssemblyContext
{
    public TimeSpan LoadTimeout { get; init; } = TimeSpan.FromSeconds(30);
    
    protected Assembly? LoadFromPath()
    {
        ThrowIfDisposed();
        ValidateLoadContext();
        
        var cts = CancellationTokenSource.CreateLinkedTokenSource(disposalTokenSource.Token);
        cts.CancelAfter(LoadTimeout);
        
        try
        {
            // Wrap in Task to enable timeout
            var loadTask = Task.Run(() => 
            {
                return this.loadContext!.LoadFromAssemblyPath(this.FilePath);
            }, cts.Token);
            
            if (!loadTask.Wait(LoadTimeout))
            {
                throw new TimeoutException(
                    $"Assembly loading timed out after {LoadTimeout.TotalSeconds} seconds: {this.FilePath}");
            }
            
            var loadedAssembly = loadTask.Result;
            // ... rest of logic
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Assembly loading was cancelled or timed out: {this.FilePath}", ex);
        }
        finally
        {
            cts.Dispose();
        }
    }
}
```

**Impact:**
- ? Prevents indefinite hangs
- ? Better resource management
- ? Configurable timeout
- ?? Adds complexity
- ?? Thread pool usage

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs`

---

### ? NET-001: Use ArgumentNullException.ThrowIfNull Consistently
**Status:** To Do  
**SSEM Pillar:** Reliability (Integrity)  
**Priority:** Low  
**Effort:** Low  

**Issue:**  
Mix of old-style and new-style null checks:

```csharp
// Old style (inconsistent)
if (String.IsNullOrEmpty(filePath)) 
    throw new ArgumentNullException(nameof(filePath), "Assembly file path cannot be null or empty");

// New style (used in some places)
ArgumentNullException.ThrowIfNull(instanceType);
```

**Proposed Standardization:**

```csharp
// For null checks only
ArgumentNullException.ThrowIfNull(instanceType, nameof(instanceType));

// For null-or-empty string checks
ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));

// For null-or-empty string checks with custom message
if (String.IsNullOrWhiteSpace(filePath))
    throw new ArgumentException("Assembly file path cannot be null or empty", nameof(filePath));
```

**Locations to Update:**
- Constructor null checks
- Method parameter validation
- Throughout the file

**Impact:**
- ? Consistency
- ? Less code
- ? Modern .NET patterns
- ? Easier to read

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (multiple locations)

---

### ? NET-002: Add Code Analysis Attributes
**Status:** To Do (Optional Enhancement)  
**SSEM Pillar:** Maintainability (Analyzability)  
**Priority:** Low  
**Effort:** Low  

**Issue:**  
Missing attributes that help static analysis tools:
- `[DoesNotReturn]` on exception-throwing methods
- `[MemberNotNull]` where appropriate
- `[return: NotNullIfNotNull]` for validation methods

**Proposed Additions:**

```csharp
using System.Diagnostics.CodeAnalysis;

[DoesNotReturn]
protected void ThrowIfDisposed()
{
    if (disposed)
    {
        throw new ObjectDisposedException(GetType().Name);
    }
}

[DoesNotReturn]
protected void ValidateLoadContext()
{
    if (this.loadContext is null)
    {
        throw new InvalidOperationException("Load context is not set.");
    }
}

[return: NotNullIfNotNull(nameof(filePath))]
public static string VerifyPath(string filePath, string basePathRestriction = "*")
{
    // ...
}

[MemberNotNull(nameof(assembly), nameof(assemblyName))]
protected Assembly LoadAssembly()
{
    // ...
}
```

**Impact:**
- ? Better static analysis
- ? Fewer false positive warnings
- ? Clearer nullability contracts
- ? Improved IDE IntelliSense

**Files to Modify:**
- `src/Xcaciv.Loader/AssemblyContext.cs` (multiple methods)

---

## Priority 5: Testing Improvements

### ? TEST-001: Add Integration Tests for Security Violations
**Status:** To Do  
**SSEM Pillar:** Trustworthiness (All sub-attributes)  
**Priority:** High  
**Effort:** Medium  

**Missing Test Coverage:**

1. **Forbidden Directory Tests**
   - Test each forbidden directory in default mode
   - Test each forbidden directory in strict mode
   - Test case-insensitive matching

2. **Path Traversal Tests**
   - Attempt to load `../../Windows/System32/kernel32.dll`
   - Attempt relative paths outside base restriction
   - Test UNC paths

3. **Wildcard Restriction Tests**
   - Verify event fires when `"*"` is used
   - Verify warning is prominent

4. **Extension Validation Tests**
   - Attempt to load `.txt`, `.exe`, `.so` files
   - Verify only `.dll` and `.exe` allowed

**Proposed Test Structure:**

```csharp
public class SecurityViolationTests
{
    [Theory]
    [InlineData(@"C:\Windows\System32\test.dll")]
    [InlineData(@"C:\Program Files\test.dll")]
    [InlineData(@"C:\Windows\System32\GroupPolicy\test.dll")]
    public void VerifyPath_ForbiddenDirectory_ThrowsSecurityException(string path)
    {
        // Arrange
        AssemblyContext.SetStrictDirectoryRestriction(true);
        
        // Act & Assert
        var ex = Assert.Throws<SecurityException>(() => 
            AssemblyContext.VerifyPath(path));
        Assert.Contains("system directories", ex.Message);
    }
    
    [Fact]
    public void Constructor_WildcardRestriction_RaisesSecurityWarning()
    {
        // Arrange
        bool eventFired = false;
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        
        // Act
        using var context = new AssemblyContext(testPath, basePathRestriction: "*");
        context.WildcardPathRestrictionUsed += (path) => eventFired = true;
        
        // Assert
        Assert.True(eventFired, "WildcardPathRestrictionUsed event should fire");
    }
    
    [Theory]
    [InlineData("test.txt")]
    [InlineData("test.so")]
    [InlineData("test.dylib")]
    public void VerifyPath_InvalidExtension_ThrowsSecurityException(string filename)
    {
        // Arrange
        var path = Path.Combine(Path.GetTempPath(), filename);
        
        // Act & Assert
        var ex = Assert.Throws<SecurityException>(() => 
            AssemblyContext.VerifyPath(path));
        Assert.Contains("extension", ex.Message);
    }
}
```

**Impact:**
- ? Verifies security controls work
- ? Regression detection
- ? Documentation through tests
- ? Confidence in security posture

**Files to Create:**
- `src/Xcaciv.LoaderTests/SecurityViolationTests.cs` (new)
- Update existing `SecurityTests.cs`

---

### ? TEST-002: Add Thread Safety Tests
**Status:** To Do  
**SSEM Pillar:** Reliability (Availability)  
**Priority:** Medium  
**Effort:** Medium  

**Missing Test Coverage:**

1. **Concurrent Loading/Unloading**
   - Multiple threads loading different assemblies
   - Multiple threads loading same assembly
   - Load/unload race conditions

2. **Event Handler Thread Safety**
   - Multiple handlers on different threads
   - Handler registration/unregistration during events
   - Event raising during disposal

**Proposed Test Structure:**

```csharp
public class ThreadSafetyTests
{
    [Fact]
    public async Task ConcurrentLoad_MultipleContexts_ThreadSafe()
    {
        // Arrange
        var tasks = new List<Task>();
        var contexts = new List<AssemblyContext>();
        
        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var context = new AssemblyContext(testDllPath, basePathRestriction: "*");
                lock (contexts) contexts.Add(context);
                context.CreateInstance<IClass1>("Class1");
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(10, contexts.Count);
        
        // Cleanup
        foreach (var context in contexts)
        {
            context.Dispose();
        }
    }
    
    [Fact]
    public async Task LoadUnload_ConcurrentOperations_NoDeadlock()
    {
        // Test for deadlocks in concurrent load/unload
    }
}
```

**Impact:**
- ? Verifies thread safety claims
- ? Detects race conditions
- ? Prevents deadlocks

**Files to Create:**
- `src/Xcaciv.LoaderTests/ThreadSafetyTests.cs` (new)

---

### ? TEST-003: Add Event Firing Tests
**Status:** To Do  
**SSEM Pillar:** Trustworthiness (Accountability, Transparency)  
**Priority:** Medium  
**Effort:** Low  

**Missing Test Coverage:**

1. **Event Timing**
   - Events fire at correct times
   - Event parameters contain expected data
   - Events fire for all code paths

2. **All Event Types**
   - `AssemblyLoaded` ?
   - `AssemblyLoadFailed` ??
   - `AssemblyUnloaded` ??
   - `SecurityViolation` ?
   - `DependencyResolved` ?
   - `WildcardPathRestrictionUsed` ?

**Proposed Test Structure:**

```csharp
public class EventTests
{
    [Fact]
    public void LoadAssembly_Success_RaisesAssemblyLoadedEvent()
    {
        // Arrange
        string? capturedPath = null;
        string? capturedName = null;
        Version? capturedVersion = null;
        
        using var context = new AssemblyContext(testDllPath, basePathRestriction: "*");
        context.AssemblyLoaded += (path, name, version) =>
        {
            capturedPath = path;
            capturedName = name;
            capturedVersion = version;
        };
        
        // Act
        context.CreateInstance<IClass1>("Class1");
        
        // Assert
        Assert.NotNull(capturedPath);
        Assert.NotNull(capturedName);
        Assert.NotNull(capturedVersion);
        Assert.Contains("TestAssembly", capturedName);
    }
    
    [Fact]
    public void VerifyPath_SecurityViolation_RaisesEvent()
    {
        // Test SecurityViolation event
    }
    
    [Fact]
    public void LoadDependentAssembly_RaisesDependencyResolvedEvent()
    {
        // Test DependencyResolved event
    }
}
```

**Impact:**
- ? Verifies audit trail works
- ? Documents event behavior
- ? Catches event regressions

**Files to Create:**
- `src/Xcaciv.LoaderTests/EventTests.cs` (new)

---

## Implementation Plan

### Phase 1: Critical Fixes (Week 1)
1. ? **COMPLETED** - REL-001: Fix silent failure in LoadFromPath
2. ? **COMPLETED** - REL-002: Reduce broad exception catching
3. ?? MAINT-003: Make security configuration instance-based
4. ?? DOC-002: Add security guidance to BasePathRestriction

### Phase 2: Maintainability (Week 2)
5. ?? MAINT-001: Refactor VerifyPath
6. ?? MAINT-004: Extract GetLoadedTypes to utility class
7. ?? DOC-001: Enhance event documentation
8. ?? NET-001: Use ArgumentNullException.ThrowIfNull consistently

### Phase 3: Testing (Week 3)
9. ?? TEST-001: Security violation tests
10. ?? TEST-003: Event firing tests
11. ?? TEST-002: Thread safety tests (if time permits)

### Phase 4: Optional Enhancements (Future)
12. ?? TRUST-001: Assembly hash verification (if requested)
13. ?? MAINT-002: Path validator interface (if testing becomes priority)
14. ?? PERF-002: Timeout support (if customer requests)
15. ?? API-001: Input sanitization helpers

---

## Breaking Changes Summary

### High Impact (Require Migration)
- **MAINT-003**: Static security configuration ? instance-based
  - `SetStrictDirectoryRestriction()` ? `AssemblySecurityPolicy`
  - Migration: Pass policy to constructor

### Medium Impact (Method Moved)
- **MAINT-004**: `GetLoadedTypes<T>()` ? `AssemblyScanner.GetLoadedTypes<T>()`
  - Migration: Change call site

### Low Impact (Behavior Change)
- **REL-001**: Silent failures now throw exceptions
  - Migration: Add try-catch if needed

---

## Compatibility Matrix

| Change | .NET 8 | .NET 10 | Breaking | Migration Effort |
|--------|--------|---------|----------|------------------|
| REL-001 | ? | ? | ?? Behavioral | Low |
| REL-002 | ? | ? | ?? Behavioral | Low |
| MAINT-001 | ? | ? | ? No | None |
| MAINT-003 | ? | ? | ? API | Medium |
| MAINT-004 | ? | ? | ? API | Low |
| All Others | ? | ? | ? No | None |

---

## Success Criteria

### SSEM Score Targets

| Pillar | Current | Target | Key Improvements |
|--------|---------|--------|------------------|
| Maintainability | 7.0 | 8.5 | Refactoring, testability |
| Trustworthiness | 9.0 | 9.5 | Documentation, events |
| Reliability | 8.0 | 9.0 | Exception handling, tests |
| **Overall** | **8.0** | **9.0** | Comprehensive improvements |

### Measurable Outcomes
- ? Zero silent failures in error paths
- ? 100% event coverage in tests
- ? All methods under 50 lines
- ? Test coverage >80%
- ? Zero static mutable state
- ? All security controls tested

---

## Notes

- This checklist prioritizes production readiness and security
- Optional enhancements can be deferred to future releases
- Breaking changes should be clearly documented in CHANGELOG
- Consider semantic versioning: these changes warrant a major version bump
- Migration guide should be provided for breaking changes

---

**Document Version:** 1.0  
**Last Updated:** 2025-11-29  
**Status:** Ready for Implementation
