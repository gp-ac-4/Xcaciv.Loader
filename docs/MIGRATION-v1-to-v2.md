# Migration Guide: v1.x to v2.0

## Overview

Version 2.0 of Xcaciv.Loader introduces **instance-based security configuration** to eliminate static mutable state and improve testability. This guide will help you migrate your code from the deprecated static API to the new instance-based approach.

## Summary of Breaking Changes

| Change | Impact | Action Required |
|--------|--------|-----------------|
| Security configuration now instance-based | High | Update constructor calls |
| Security exceptions now propagate | Medium | Add try-catch if needed |
| Static methods deprecated | Low | Replace with instance properties |

---

## 1. Security Policy Configuration

### Before (v1.x - Deprecated)

```csharp
// Global static configuration affecting all instances
AssemblyContext.SetStrictDirectoryRestriction(true);

var context1 = new AssemblyContext(pluginPath1, basePathRestriction: dir1);
var context2 = new AssemblyContext(pluginPath2, basePathRestriction: dir2);
// Both contexts use strict mode
```

### After (v2.0 - Recommended)

```csharp
// Per-instance configuration
var context1 = new AssemblyContext(
    pluginPath1,
    basePathRestriction: dir1,
    securityPolicy: AssemblySecurityPolicy.Strict);

var context2 = new AssemblyContext(
    pluginPath2,
    basePathRestriction: dir2,
    securityPolicy: AssemblySecurityPolicy.Default);
// Each context has independent security policy
```

### Migration Steps

1. **Find all calls to `SetStrictDirectoryRestriction`**
   ```bash
   # Search your codebase
   grep -r "SetStrictDirectoryRestriction" .
   ```

2. **Determine the desired security level**
   - Was strict mode enabled? ? Use `AssemblySecurityPolicy.Strict`
   - Was strict mode disabled? ? Use `AssemblySecurityPolicy.Default` (or omit parameter)

3. **Update constructor calls**
   ```csharp
   // OLD
   AssemblyContext.SetStrictDirectoryRestriction(true);
   var context = new AssemblyContext(path, basePathRestriction: dir);
   
   // NEW
   var context = new AssemblyContext(
       path,
       basePathRestriction: dir,
       securityPolicy: AssemblySecurityPolicy.Strict);
   ```

4. **Remove static configuration calls**
   ```csharp
   // DELETE these lines
   AssemblyContext.SetStrictDirectoryRestriction(true);
   AssemblyContext.SetStrictDirectoryRestriction(false);
   ```

---

## 2. Checking Security Mode

### Before (v1.x - Deprecated)

```csharp
if (AssemblyContext.IsStrictDirectoryRestrictionEnabled())
{
    // Handle strict mode
}
```

### After (v2.0 - Recommended)

```csharp
if (context.SecurityPolicy.StrictMode)
{
    // Handle strict mode
}
```

### Migration Steps

1. **Replace static checks with instance property**
   ```csharp
   // OLD
   bool isStrict = AssemblyContext.IsStrictDirectoryRestrictionEnabled();
   
   // NEW  
   bool isStrict = context.SecurityPolicy.StrictMode;
   ```

---

## 3. Custom Security Policies (New Feature)

V2.0 introduces the ability to define custom forbidden directories:

```csharp
// Define custom policy
var customPolicy = new AssemblySecurityPolicy(
    forbiddenDirectories: new[] { 
        "temp", 
        "downloads", 
        "desktop",
        "appdata\\local\\temp"
    });

var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    securityPolicy: customPolicy);
```

**Use cases:**
- Blocking specific directories beyond the defaults
- Environment-specific restrictions
- Compliance requirements

---

## 4. Exception Handling Changes

### Before (v1.x)

```csharp
// Security violations during dependency resolution returned null silently
var context = new AssemblyContext(path, basePathRestriction: "*");
var instance = context.CreateInstance<IPlugin>("PluginClass");
// Might be null if dependency had security violation
```

### After (v2.0)

```csharp
// Security violations now throw SecurityException
try
{
    var context = new AssemblyContext(path, basePathRestriction: "*");
    var instance = context.CreateInstance<IPlugin>("PluginClass");
}
catch (SecurityException ex)
{
    // Handle security violation
    logger.LogError("Security violation: {Message}", ex.Message);
}
```

### Migration Steps

**If you relied on silent failures:**

1. **Identify code that expects null returns**
   ```csharp
   // OLD (might silently fail)
   var instance = context.CreateInstance<IPlugin>("PluginClass");
   if (instance is null)
   {
       // Handle failure
   }
   ```

2. **Add exception handling**
   ```csharp
   // NEW (explicit error handling)
   try
   {
       var instance = context.CreateInstance<IPlugin>("PluginClass");
   }
   catch (SecurityException ex)
   {
       logger.LogWarning("Plugin load blocked: {Message}", ex.Message);
       // Handle security violation
   }
   catch (TypeNotFoundException ex)
   {
       logger.LogWarning("Plugin type not found: {Message}", ex.Message);
       // Handle missing type
   }
   ```

---

## 5. Testing Updates

### Before (v1.x)

```csharp
[Fact]
public void Test1()
{
    AssemblyContext.SetStrictDirectoryRestriction(true);
    // Test code
    AssemblyContext.SetStrictDirectoryRestriction(false); // Cleanup
}

[Fact]  
public void Test2()
{
    // Might interfere with Test1 if run in parallel
}
```

### After (v2.0)

```csharp
[Fact]
public void Test1()
{
    var context = new AssemblyContext(
        path,
        securityPolicy: AssemblySecurityPolicy.Strict);
    // Test code
    // No cleanup needed - instance is isolated
}

[Fact]
public void Test2()
{
    // Completely isolated - safe for parallel execution
}
```

### Benefits

- ? **Parallel test execution safe**
- ? **No test cleanup required**
- ? **No global state pollution**
- ? **Easier to test different security scenarios**

---

## 6. Complete Migration Example

### Before (v1.x)

```csharp
public class PluginLoader
{
    private readonly string pluginDirectory;
    
    public PluginLoader(string pluginDirectory, bool useStrictMode)
    {
        this.pluginDirectory = pluginDirectory;
        AssemblyContext.SetStrictDirectoryRestriction(useStrictMode);
    }
    
    public IPlugin LoadPlugin(string pluginPath)
    {
        var context = new AssemblyContext(
            pluginPath,
            basePathRestriction: pluginDirectory);
            
        var plugin = context.CreateInstance<IPlugin>("Plugin");
        return plugin; // Might be null
    }
    
    public bool IsStrictMode()
    {
        return AssemblyContext.IsStrictDirectoryRestrictionEnabled();
    }
}
```

### After (v2.0)

```csharp
public class PluginLoader
{
    private readonly string pluginDirectory;
    private readonly AssemblySecurityPolicy securityPolicy;
    
    public PluginLoader(
        string pluginDirectory, 
        AssemblySecurityPolicy? securityPolicy = null)
    {
        this.pluginDirectory = pluginDirectory;
        this.securityPolicy = securityPolicy ?? AssemblySecurityPolicy.Default;
    }
    
    public IPlugin LoadPlugin(string pluginPath)
    {
        try
        {
            var context = new AssemblyContext(
                pluginPath,
                basePathRestriction: pluginDirectory,
                securityPolicy: securityPolicy);
                
            return context.CreateInstance<IPlugin>("Plugin");
        }
        catch (SecurityException ex)
        {
            throw new PluginLoadException(
                $"Security violation loading plugin: {ex.Message}", 
                ex);
        }
        catch (TypeNotFoundException ex)
        {
            throw new PluginLoadException(
                $"Plugin type not found: {ex.Message}", 
                ex);
        }
    }
    
    public bool IsStrictMode()
    {
        return securityPolicy.StrictMode;
    }
}

// Usage
var loader = new PluginLoader(
    pluginDir, 
    AssemblySecurityPolicy.Strict);
```

---

## 7. Compiler Warnings

The deprecated methods will produce compiler warnings:

```
CS0618: 'AssemblyContext.SetStrictDirectoryRestriction(bool)' is obsolete: 
'Use AssemblySecurityPolicy instead. Pass AssemblySecurityPolicy.Strict or 
AssemblySecurityPolicy.Default to the constructor.'
```

**To suppress warnings during migration:**
```csharp
#pragma warning disable CS0618
AssemblyContext.SetStrictDirectoryRestriction(true);
#pragma warning restore CS0618
```

**Better: Complete the migration to remove warnings permanently**

---

## 8. Rollback Plan

If you need to rollback to v1.x:

1. **Revert package version**
   ```xml
   <PackageReference Include="Xcaciv.Loader" Version="1.0.2" />
   ```

2. **Restore static configuration**
   ```csharp
   // v1.x code works unchanged
   AssemblyContext.SetStrictDirectoryRestriction(true);
   var context = new AssemblyContext(path, basePathRestriction: dir);
   ```

3. **Consider staying on v1.x if:**
   - You have time constraints for migration
   - Your codebase is very large
   - You don't need parallel testing or per-instance policies

**Note**: v1.x will only receive security fixes until 2026-06-01

---

## 9. Migration Checklist

Use this checklist to track your migration progress:

### Code Updates
- [ ] Find all `SetStrictDirectoryRestriction` calls
- [ ] Replace with `securityPolicy` constructor parameter
- [ ] Find all `IsStrictDirectoryRestrictionEnabled` calls
- [ ] Replace with `context.SecurityPolicy.StrictMode`
- [ ] Review exception handling for security violations
- [ ] Add try-catch where silent failures were expected
- [ ] Update unit tests to use instance-based policies
- [ ] Remove test cleanup code (no longer needed)

### Documentation
- [ ] Update internal documentation
- [ ] Update code examples
- [ ] Update deployment guides
- [ ] Inform team of changes

### Testing
- [ ] Run existing tests
- [ ] Verify parallel test execution works
- [ ] Test with strict policy
- [ ] Test with default policy
- [ ] Test with custom policy (if used)
- [ ] Verify security violations are caught

### Deployment
- [ ] Stage environment testing
- [ ] Production deployment plan
- [ ] Rollback procedure documented
- [ ] Monitor for issues post-deployment

---

## 10. Support

### Getting Help

- **GitHub Issues**: https://github.com/Xcaciv/Xcaciv.Loader/issues
- **Documentation**: See `readme.md` in the package
- **Examples**: See `examples/` directory

### Reporting Migration Issues

When reporting issues, please include:
1. Version you're migrating from
2. Version you're migrating to
3. Code sample (before and after)
4. Error message or unexpected behavior
5. Environment details (.NET version, OS)

---

## 11. FAQ

**Q: Can I use both old and new APIs during migration?**  
A: Yes, deprecated methods still work but will show compiler warnings. You can suppress warnings and migrate incrementally.

**Q: Will v1.x continue to be supported?**  
A: Security fixes only until 2026-06-01. Feature development is on v2.x+.

**Q: Do I need to change my code if I wasn't using strict mode?**  
A: Not required but recommended. Default behavior is unchanged if you don't pass `securityPolicy` parameter.

**Q: Can I use different policies for different plugins?**  
A: Yes! This is a key benefit of v2.0. Each `AssemblyContext` can have its own policy.

**Q: Will this break my existing deployed applications?**  
A: Only if you upgrade the package. V1.x applications continue to work. Plan your upgrade carefully.

**Q: How do I test the migration?**  
A: Create a branch, update one module at a time, run tests, verify behavior matches expectations.

---

## 12. Timeline

| Date | Milestone |
|------|-----------|
| 2025-11-29 | v2.0.0 released with deprecation warnings |
| 2026-03-01 | Deprecation warnings become errors in new v2.x releases |
| 2026-06-01 | v1.x reaches end of life (security fixes cease) |
| 2026-09-01 | v3.0.0 removes deprecated methods entirely |

**Recommendation**: Complete migration before 2026-03-01 to avoid breaking changes.

---

## Conclusion

The migration to instance-based security configuration provides:
- ? Better testability
- ? Thread safety
- ? Per-context security policies
- ? Cleaner architecture
- ? SSEM compliance

While it requires code changes, the benefits significantly outweigh the migration effort. Most migrations can be completed in 1-2 days depending on codebase size.

**Start your migration today to take advantage of these improvements!**
