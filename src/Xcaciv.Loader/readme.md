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
- Instance-based security configuration for flexible multi-context scenarios
- Optional cryptographic hash-based assembly integrity verification (disabled by default)

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
// Use strict security policy (recommended for production)
var context = new AssemblyContext(
    dllPath, 
    basePathRestriction: pluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict);

// In strict mode, these paths are automatically blocked:
// - C:\Windows\System32
// - C:\Program Files
// - C:\ProgramData
// - Credential stores
// - Windows Defender directories
// - And other sensitive system directories

// Any attempt to load from blocked directories will throw SecurityException
```

#### Per-Instance Security Policies

Different contexts can use different security policies:

```csharp
// Trusted plugins with default security
var trustedContext = new AssemblyContext(
    trustedPluginPath,
    basePathRestriction: trustedPluginDir,
    securityPolicy: AssemblySecurityPolicy.Default);

// Untrusted plugins with strict security
var untrustedContext = new AssemblyContext(
    untrustedPluginPath,
    basePathRestriction: untrustedPluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict);

// Custom security policy
var customPolicy = new AssemblySecurityPolicy(new[] { "temp", "downloads", "desktop" });
var customContext = new AssemblyContext(
    customPluginPath,
    basePathRestriction: customPluginDir,
    securityPolicy: customPolicy);
```

#### Migration from Static API (Deprecated)

The old static `SetStrictDirectoryRestriction` method is deprecated:

```csharp
// ? OLD (Deprecated): Global static configuration
AssemblyContext.SetStrictDirectoryRestriction(true);
var context = new AssemblyContext(dllPath, basePathRestriction: pluginDir);

// ? NEW: Instance-based configuration
var context = new AssemblyContext(
    dllPath,
    basePathRestriction: pluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict);
```

**Benefits of instance-based approach:**
- No global state - parallel test execution safe
- Different security policies per context
- More flexible and testable
- Thread-safe by design

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
```

---

## Assembly Integrity Verification

**Optional Feature**: Cryptographic hash-based verification to protect against tampered assemblies.

### When to Use

- High-security environments
- When loading assemblies from untrusted sources
- Compliance requirements (SOC 2, ISO 27001)
- Defense-in-depth security strategy

### Learning Mode (Recommended for Development)

Automatically trust assemblies on first load:

```csharp
// Create verifier in learning mode
var verifier = new AssemblyIntegrityVerifier(
    enabled: true,
    learningMode: true,
    algorithm: HashAlgorithmName.SHA256);

// Load assemblies - hashes are learned automatically
var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    integrityVerifier: verifier);

// Save learned hashes for production
verifier.HashStore.SaveToFile("trusted-hashes.csv");
```

### Strict Mode (Production)

Only load assemblies with known hashes:

```csharp
// Load pre-computed hashes
var store = new AssemblyHashStore();
store.LoadFromFile("trusted-hashes.csv");

// Create verifier in strict mode (no learning)
var verifier = new AssemblyIntegrityVerifier(
    enabled: true,
    learningMode: false,
    hashStore: store);

// This will throw SecurityException if hash doesn't match
var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    integrityVerifier: verifier);
```

### Pre-Computing Hashes

Manually trust specific assemblies:

```csharp
var verifier = new AssemblyIntegrityVerifier(
    enabled: true,
    learningMode: false);

// Trust specific assemblies
verifier.TrustAssembly(@"C:\Plugins\TrustedPlugin.dll");
verifier.TrustAssembly(@"C:\Plugins\AnotherPlugin.dll");

// Save to file
verifier.HashStore.SaveToFile("trusted-hashes.csv");
```

### Monitoring Integrity Events

```csharp
var verifier = new AssemblyIntegrityVerifier(enabled: true, learningMode: true);

// Monitor hash mismatches
verifier.HashMismatchDetected += (path, expected, actual) =>
{
    securityLogger.LogCritical(
        "Assembly tampered! Path: {Path}, Expected: {Expected}, Actual: {Actual}",
        path, expected, actual);
    alertingService.RaiseSecurityAlert("AssemblyTampered", path);
};

// Monitor hash learning
verifier.HashLearned += (path, hash) =>
{
    auditLogger.LogInformation(
        "New assembly trusted: {Path}, Hash: {Hash}",
        path, hash);
};

var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    integrityVerifier: verifier);
```

### Hash Store File Format

Simple CSV format (path,hash):

```csv
# Assembly Integrity Hash Store
# Format: FilePath,Hash
C:\Plugins\Plugin1.dll,abc123base64hash==
C:\Plugins\Plugin2.dll,def456base64hash==
```

**Path Handling:**
- Paths are **case-sensitive** (e.g., `Plugin.dll` and `plugin.dll` are treated as different files)
- Relative paths are converted to absolute paths automatically
- Path separators are normalized (backslashes on Windows, forward slashes on Unix)
- Original path casing is preserved in the CSV file

**Note for Cross-Platform:**
- On Windows (case-insensitive file system), different case variations refer to the same physical file but are tracked separately in the hash store
- On Linux/Unix (case-sensitive file systems), different case variations are truly different files
- For consistency, always use the same casing when referring to assembly paths

### Supported Hash Algorithms

- SHA256 (default, recommended)
- SHA384
- SHA512

### Performance Considerations

- Hash computation occurs once per assembly load
- SHA256 is fast (~100MB/s) with minimal overhead
- Use learning mode in development, strict mode in production
- Store hash database securely (read-only in production)

---