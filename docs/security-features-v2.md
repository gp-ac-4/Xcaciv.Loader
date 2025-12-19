# Security Features (v2.0+)

This document summarizes the security-oriented capabilities introduced and enhanced in Xcaciv.Loader v2.0 and later. It focuses on policies, enforcement points, audit trail, and recommended practices for secure dynamic assembly loading.

## Overview

- Defense-in-depth approach combining input validation, directory restrictions, integrity verification, and audit events.
- Instance-based `AssemblySecurityPolicy` for per-context control.
- Optional cryptographic integrity verification (`AssemblyIntegrityVerifier`).
- Comprehensive audit events to surface violations and load activity.

## Security Policies

- **`AssemblySecurityPolicy.Default`**: Basic restrictions against sensitive system directories (e.g., Group Policy, System Profile).
- **`AssemblySecurityPolicy.Strict`**: Enhanced restrictions including Windows system folders, credentials-related locations, event logs, etc. In v2.1+, Strict also enables dynamic assembly blocking by default.
- **Custom Policies**: Provide a tailored set of forbidden directories via `new AssemblySecurityPolicy(string[] forbiddenDirectories)`. Defaults keep dynamic assembly loading allowed unless you explicitly opt-in.

### Dynamic Assembly Blocking (v2.1+)

- **Flag**: `DisallowDynamicAssemblies` on `AssemblySecurityPolicy`.
- **Enforcement**: `AssemblyContext` rejects dynamic/in-memory assemblies (e.g., created via Reflection.Emit) during load operations when this flag is enabled. Violations raise `SecurityViolation` then throw `SecurityException`.
- **Defaults**:
  - `Strict`: `DisallowDynamicAssemblies = true`.
  - `Default`: `DisallowDynamicAssemblies = false` (backward compatibility).
- **Limitations**: Blocking is local to loads performed through `AssemblyContext`. It cannot prevent other code in the process from using Reflection.Emit. For stronger isolation, consider separate processes or NativeAOT scenarios.

## Global Dynamic Assembly Monitoring (Audit-Only)

In addition to local enforcement, v2.1 introduces opt-in global monitoring to improve transparency:

- **Enable**: Call `EnableGlobalDynamicAssemblyMonitoring()` on your `AssemblyContext` instance(s).
- **Mechanism**: Subscribes a single, shared `AppDomain.CurrentDomain.AssemblyLoad` handler. When any dynamic assembly is observed, subscribed contexts that disallow dynamic assemblies raise `SecurityViolation` events with a descriptive reason.
- **Audit-Only**: This hook does not throw or block loads. It is designed for telemetry and incident detection across the AppDomain.
- **Lifecycle**: Uses weak references; the handler detaches when no subscribers remain. Contexts automatically remove themselves on dispose.

## Path Validation and Directory Restrictions

- **Path Verification**: `AssemblyContext.VerifyPath()` enforces:
  - Normalization and extension checks (`.dll`/`.exe`).
  - Base path restriction (`"."` by default). Use explicit plugin directories in production.
  - Forbidden directory detection via `AssemblySecurityPolicy.ContainsForbiddenDirectory()`.
- **Wildcard Warning**: `"*"` disables base path restriction and is unsafe in production. A warning event (`WildcardPathRestrictionUsed`) is raised to surface risk.

## Integrity Verification (Optional)

- **Components**: `AssemblyIntegrityVerifier` (engine) + `AssemblyHashStore` (CSV-backed store).
- **Modes**:
  - Learning mode (development): trusts first load and records the hash.
  - Strict mode (production): only loads assemblies with known, matching hashes.
- **Events**: Hash mismatch and learning events provide auditability.

## Audit Events

- **`AssemblyLoaded`**: Successful load (name, version, path).
- **`AssemblyLoadFailed`**: Failure with the causing exception.
- **`SecurityViolation`**: Detected violation (reason and path/identifier).
- **`DependencyResolved`**: Where dependencies were found.
- **`AssemblyUnloaded`**: Unload attempt result.
- **`WildcardPathRestrictionUsed`**: Warning when unrestricted base path is used.

## Recommended Practices

- **Always set an explicit base path** for production (e.g., a dedicated plugin directory).
- **Use `AssemblySecurityPolicy.Strict`** in production to enable stronger directory restrictions and dynamic assembly blocking.
- **Enable global monitoring** for telemetry in high-assurance environments; treat it as audit-only.
- **Enable integrity verification** in production and manage hashes via source-controlled artifacts or secure storage.
- **Handle events** to log, alert, and investigate violations and failures.

## Usage Examples

### Strict policy with dynamic blocking

```csharp
var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict);
```

### Custom policy enabling dynamic blocking

```csharp
var policy = new AssemblySecurityPolicy(new[] { "plugins", "extensions" })
{
    DisallowDynamicAssemblies = true
};

var context = new AssemblyContext(pluginPath, basePathRestriction: pluginDir, securityPolicy: policy);
```

### Opt-in global monitoring (audit-only)

```csharp
var context = new AssemblyContext(pluginPath, basePathRestriction: pluginDir, securityPolicy: AssemblySecurityPolicy.Strict);
context.EnableGlobalDynamicAssemblyMonitoring();

context.SecurityViolation += (id, reason) =>
{
    logger.LogCritical("SECURITY VIOLATION (Global): {Reason} - Identifier: {Id}", reason, id);
};
```

### Integrity verification (strict mode)

```csharp
var store = new AssemblyHashStore();
store.LoadFromFile("trusted-hashes.csv");

var verifier = new AssemblyIntegrityVerifier(enabled: true, learningMode: false, hashStore: store);

var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict,
    integrityVerifier: verifier);
```

---

For migration details and broader documentation, see:

- [docs/MIGRATION-v1-to-v2.md](MIGRATION-v1-to-v2.md)
- [docs/README.md](README.md)
