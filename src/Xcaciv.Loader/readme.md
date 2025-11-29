# Xcaciv.Loader

Sexy simple C# module for runtime loading of types from external assemblies. This is a separate assembly to abstract AssemblyLoader operations and suppress trimming alerts.

## ?? Security Notice

**When loading assemblies dynamically, always restrict the base path in production environments.** Using wildcard `"*"` allows loading from ANY location, including system directories, which can lead to arbitrary code execution vulnerabilities.

## Basic Usage

```csharp
// ? RECOMMENDED: Explicit path restriction
using (var context = new AssemblyContext(
    dllPath, 
    basePathRestriction: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins")))
{
    var myInstance = context.CreateInstance<IClass1>("Class1");
    return myInstance.Stuff("input here");
} // Automatically unloads

// ? SECURE: Default restricts to current directory
using (var context = new AssemblyContext(dllPath))
{
    var myInstance = context.CreateInstance<IClass1>("Class1");
    return myInstance.Stuff("input here");
}
```

## Features

- Dynamic assembly loading and unloading
- Type discovery and instantiation
- Security measures to prevent loading from restricted directories
- Automatic dependency resolution
- Optional event-based audit trail for security compliance (SSEM)
- Configurable security policies (default and strict modes)

## Security Best Practices

### ?? Critical: Path Restrictions

The `basePathRestriction` parameter is your primary defense against arbitrary code execution. **Never use wildcard `"*"` in production.**

#### ? Recommended Patterns

```csharp
// BEST: Dedicated plugin directory
var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
var context = new AssemblyContext(dllPath, basePathRestriction: pluginDir);

// GOOD: Application base directory
var context = new AssemblyContext(dllPath, basePathRestriction: AppDomain.CurrentDomain.BaseDirectory);

// GOOD: Default (current directory)
var context = new AssemblyContext(dllPath);
```

#### ? Dangerous Patterns

```csharp
// DANGEROUS: Wildcard allows loading from ANYWHERE
var context = new AssemblyContext(dllPath, basePathRestriction: "*");
// ?? This is equivalent to no security - only use in isolated test environments!

// RISKY: User-controlled paths without validation
var userPath = Request.QueryString["pluginPath"]; // User input
var context = new AssemblyContext(userPath, basePathRestriction: "*");
// ?? Allows arbitrary code execution!
```

#### Security Boundary Guidelines

1. **Production**: Always use explicit directory paths
2. **Staging**: Use application base directory or dedicated folders
3. **Development**: Can use broader paths but still restrict to project directories
4. **Testing**: Wildcard `"*"` acceptable ONLY in isolated, automated test environments

### Strict Directory Restriction Mode

Enable enhanced security checks to block loading from system directories:

```csharp
// Enable strict mode globally (recommended for production)
AssemblyContext.SetStrictDirectoryRestriction(true);

// In strict mode, these paths are automatically blocked:
// - C:\Windows\System32
// - C:\Program Files
// - C:\ProgramData
// - Credential stores
// - Windows Defender directories
// - And other sensitive system directories

var context = new AssemblyContext(dllPath, basePathRestriction: pluginDir);
// Any attempt to load from blocked directories will throw SecurityException
```

### Monitor Security Events

Always subscribe to `SecurityViolation` and `WildcardPathRestrictionUsed` events in production:

```csharp
var context = new AssemblyContext(dllPath, basePathRestriction: pluginDir);

// Monitor security violations
context.SecurityViolation += (path, reason) =>
{
    // Log to security audit system
    securityLogger.LogWarning(
        "Security violation detected. Path: {Path}, Reason: {Reason}", 
        path, reason);
    
    // Optional: Alert security team for immediate response
    alertingService.RaiseSecurityAlert(new SecurityAlert
    {
        Type = "UnauthorizedAssemblyLoad",
        Severity = AlertSeverity.High,
        Path = path,
        Reason = reason
    });
};

// Monitor wildcard usage (should never fire in production)
context.WildcardPathRestrictionUsed += (path) =>
{
    securityLogger.LogCritical(
        "CRITICAL: Wildcard path restriction used in production! Path: {Path}", 
        path);
    // This indicates a security misconfiguration
};
```

### Defense in Depth

Layer multiple security controls:

```csharp
// 1. Enable strict mode
AssemblyContext.SetStrictDirectoryRestriction(true);

// 2. Use explicit path restriction
var pluginDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");

// 3. Subscribe to security events
var context = new AssemblyContext(dllPath, basePathRestriction: pluginDir);
context.SecurityViolation += LogSecurityViolation;
context.WildcardPathRestrictionUsed += LogWildcardUsage;

// 4. Verify assembly signatures (future enhancement)
// 5. Use least-privilege principles for the application process