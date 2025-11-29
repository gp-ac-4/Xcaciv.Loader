# Xcaciv.Loader

Sexy simple C# module for runtime loading of types from external assemblies. This is a separate assembly to abstract AssemblyLoader operations and suppress trimming alerts.

## Basic Usage

```csharp
using (var context = new AssemblyContext(dllPath, basePathRestriction: AppDomain.CurrentDomain.BaseDirectory))
{
    var myInstance = context.CreateInstance<IClass1>("Class1");
    return myInstance.Stuff("input here");
} // Automatically unloads
```

## Features

- Dynamic assembly loading and unloading
- Type discovery and instantiation
- Security measures to prevent loading from restricted directories
- Automatic dependency resolution
- Optional event-based audit trail for security compliance (SSEM)

## Event-Based Logging (Optional)

Subscribe to events for audit trails, security monitoring, and transparency:

```csharp
var context = new AssemblyContext(dllPath, basePathRestriction: basePath);

// Subscribe to security events
context.SecurityViolation += (path, reason) => 
    Console.WriteLine($"SECURITY: Blocked loading from {path}: {reason}");

context.WildcardPathRestrictionUsed += (path) => 
    Console.WriteLine($"WARNING: Wildcard restriction used for {path}");

// Subscribe to assembly lifecycle events
context.AssemblyLoaded += (path, name, version) => 
    Console.WriteLine($"Loaded: {name} v{version} from {path}");

context.AssemblyLoadFailed += (path, ex) => 
    Console.WriteLine($"Failed to load from {path}: {ex.Message}");

context.AssemblyUnloaded += (path, success) => 
    Console.WriteLine($"Unload {(success ? "succeeded" : "failed")} for {path}");

context.DependencyResolved += (depName, resolvedPath) => 
    Console.WriteLine($"Resolved dependency: {depName} -> {resolvedPath}");

using (context)
{
    var instance = context.CreateInstance<IMyInterface>("MyClass");
    instance.DoWork();
}
```

## Security Best Practices

### Path Restrictions

Always specify an explicit `basePathRestriction` in production:

```csharp
// GOOD - Restricts loading to specific directory
var context = new AssemblyContext(
    dllPath, 
    basePathRestriction: @"C:\MyApp\Plugins");

// BAD - Allows loading from any directory (use only in tests)
var context = new AssemblyContext(
    dllPath, 
    basePathRestriction: "*");
```

### Strict Directory Restriction Mode

Enable enhanced security checks to block loading from system directories:

```csharp
// Enable strict mode globally
AssemblyContext.SetStrictDirectoryRestriction(true);

// Now blocks loading from:
// - C:\Windows\System32
// - C:\Program Files
// - Credential stores
// - And other sensitive system directories
```

### Monitor Security Events

Always subscribe to `SecurityViolation` events in production to detect unauthorized loading attempts:

```csharp
context.SecurityViolation += (path, reason) =>
{
    // Log to security audit system
    securityLogger.LogWarning($"Attempted to load from restricted path: {path}. Reason: {reason}");
    // Optional: Alert security team
    alertingService.RaiseSecurityAlert("UnauthorizedAssemblyLoad", path);
};
```

## Specification

For detailed specifications, see [the specification document](../../docs/spec-architecture-dynamic-assembly-loading.md)