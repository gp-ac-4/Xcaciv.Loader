# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Comprehensive Testing for Dynamic Assembly Monitoring**:
  - New test project `zTestRiskyAssembly`: Assembly demonstrating Reflection.Emit usage for testing workflows
  - New test project `zTestLinqExpressions`: Assembly demonstrating LINQ Expressions Compile usage for testing workflows
  - Comprehensive test suite `GlobalDynamicAssemblyMonitoringTests` with 15+ tests covering:
    - Basic monitoring functionality with Strict vs Default policies
    - Multi-context monitoring with independent violation tracking
    - Weak reference cleanup and disposed context handling
    - Concurrency and thread-safety of EnableGlobalDynamicAssemblyMonitoring
    - Event message content verification
    - Duplicate subscription prevention
    - Integration tests with actual risky assemblies
  - Comprehensive test suite `DisallowDynamicAssembliesTests` with 13+ tests covering:
    - Policy configuration (Strict, Default, Custom)
    - Context configuration with different policies
    - Multi-context isolation
    - Policy inheritance in strict mode

### Changed
- Added `InternalsVisibleTo` attribute to allow test projects to access internal `AssemblyPreflightAnalyzer` functionality
- Refactored `MergeFromFile` in `AssemblyHashStore` to use explicit LINQ `Where` filter for better code clarity
- Improved condition logic in `UnloadAsync` method to fix always-false evaluation issue
- Minor documentation updates in readme

### Fixed
- Fixed always-false condition in `UnloadAsync` method that could prevent proper cleanup
- Improved LINQ query readability in `AssemblyHashStore.MergeFromFile`

### Testing
- New test assembly projects for workflow testing:
  - `zTestRiskyAssembly.DynamicTypeCreator`: Creates dynamic types using AssemblyBuilder and TypeBuilder
  - `zTestLinqExpressions.ExpressionCompiler`: Compiles LINQ expression trees at runtime
- 28+ new tests for dynamic assembly monitoring and policies
- Test coverage for edge cases: disposed contexts, concurrent calls, weak reference cleanup
- Integration tests demonstrating real-world usage patterns

---

## [2.1.0] - 2025-12-22

### Added
- New security policy flag: `AssemblySecurityPolicy.DisallowDynamicAssemblies` to prohibit loading dynamic/in-memory assemblies (e.g., created via Reflection.Emit such as `AssemblyBuilder`, `TypeBuilder`, `DynamicMethod`).

### Changed
- `AssemblyContext` enforces `DisallowDynamicAssemblies` during load operations (`LoadFromPath`, `LoadFromName`): raises `SecurityViolation` and throws `SecurityException` when policy disallows dynamic assemblies.
- `AssemblySecurityPolicy.Strict` enables `DisallowDynamicAssemblies` by default; `Default` leaves it disabled for compatibility.

### Security
- Blocks dynamic/in-memory assembly loads under strict policy to reduce runtime injection risk.

### Notes
- No breaking changes for `Default` policy users. `Strict` may now throw `SecurityException` when attempting to load dynamic assemblies.


## [2.0.0] - 2025-11-30

### Added
- **Instance-based Security Policies**: New `AssemblySecurityPolicy` class provides configurable security policies per `AssemblyContext` instance
  - `AssemblySecurityPolicy.Default`: Basic system directory restrictions
  - `AssemblySecurityPolicy.Strict`: Enhanced restrictions for high-security environments
  - Support for custom forbidden directory lists
  - `SecurityPolicy` property on `AssemblyContext` for per-instance configuration
- **Assembly Integrity Verification**: Optional cryptographic hash-based verification (disabled by default)
  - `AssemblyIntegrityVerifier` class with learning and strict modes
  - `AssemblyHashStore` for managing hashes with CSV persistence
  - Support for SHA256, SHA384, and SHA512 hash algorithms
  - Learning mode automatically trusts new assemblies on first load
  - Strict mode only loads assemblies with known hashes
  - Events for hash mismatches and hash learning
  - Simple CSV file format (no external dependencies)
  - 62 comprehensive tests covering all scenarios
- **Path Validation Utilities**: New `AssemblyPathValidator` class for input sanitization
  - `SanitizeAssemblyPath()`: Remove dangerous characters, normalize separators
  - `ResolveRelativeToBase()`: Resolve relative paths to application base
  - `IsSafePath()`: Basic heuristic safety checks
  - `HasValidAssemblyExtension()`: Validate .dll or .exe extension
  - `ValidateAndSanitize()`: Combined validation pipeline (recommended)
  - 28 unit tests covering all methods
- **Type Discovery Utilities**: New `AssemblyScanner` class for clean type scanning
  - `GetLoadedTypes<T>()`: Scan all loaded assemblies in AppDomain
  - `GetTypes<T>(Assembly)`: Scan specific assembly
  - Better organization than mixing with `AssemblyContext`
- **Enhanced Exception Handling**: Specific exception types now caught and wrapped with context:
  - `MissingMethodException`: Type lacks parameterless constructor
  - `TargetInvocationException`: Constructor threw an exception
  - `MemberAccessException`: Cannot access constructor  
  - `TypeLoadException`: Type could not be loaded
- **Comprehensive Event Documentation**: Professional-grade XML documentation for all 6 events
  - Thread safety guarantees explicitly documented
  - Timing information (when events fire in lifecycle)
  - Parameter descriptions with types
  - Practical code examples for each event
  - Security guidance where applicable
  - Better IntelliSense experience
- **Comprehensive Security Documentation**: 
  - Prominent security warnings in XML documentation
  - Detailed examples of secure vs insecure patterns
  - Migration guide for deprecated APIs (12 sections)
  - Defense-in-depth security strategies
  - Assembly integrity verification guide
  - Complete user and developer documentation
- **Audit Trail Events**: All security violations and dependency resolutions now raise events before throwing exceptions
- **Documentation Index**: New `docs/README.md` provides navigation for all documentation

### Changed
- **BREAKING**: Security configuration is now instance-based instead of static
  - Each `AssemblyContext` can have its own `SecurityPolicy`
  - Enables parallel test execution without interference
  - Thread-safe by design (no shared mutable state)
- **BREAKING**: Silent failures eliminated - security exceptions now always propagate after raising events
  - `LoadFromPath` helper method now re-throws `SecurityException` after audit event
  - Dependency resolution failures are now visible to callers
- **Code Modernization**: Null checking standardized to use .NET helper methods
  - `ArgumentNullException.ThrowIfNull()` for null-only checks
  - `ArgumentException.ThrowIfNullOrWhiteSpace()` for string checks
  - Consistent pattern throughout codebase (4 locations updated)
  - Improved code readability and reduced boilerplate
  - Follows modern .NET best practices
- Exception handling improved throughout:
  - Replaced broad `catch (Exception ex) when (ex is not ...)` patterns
  - Added specific exception types with clear error messages
  - Preserved exception context through proper wrapping
- Event documentation significantly enhanced with examples and guidance
- All XML documentation follows professional standards

### Deprecated  
- `AssemblyContext.SetStrictDirectoryRestriction(bool)`: Use `AssemblySecurityPolicy` parameter in constructor instead
  - **Removal planned**: v3.0.0 (2026-06-01)
  - **Migration guide**: See docs/MIGRATION-v1-to-v2.md
- `AssemblyContext.IsStrictDirectoryRestrictionEnabled()`: Use `AssemblyContext.SecurityPolicy.StrictMode` property instead
  - **Removal planned**: v3.0.0 (2026-06-01)
- `AssemblyContext.GetLoadedTypes<T>()`: Use `AssemblyScanner.GetLoadedTypes<T>()` instead
  - **Removal planned**: v3.0.0 (2026-06-01)
  - **Rationale**: Better separation of concerns

### Migration Guide

#### From Static to Instance-Based Security Configuration

**Before (v1.0.x):**
```csharp
// Global static configuration
AssemblyContext.SetStrictDirectoryRestriction(true);
var context = new AssemblyContext(
    dllPath, 
    basePathRestriction: pluginDir);
```

**After (v2.0.0):**
```csharp
// Instance-based configuration
var context = new AssemblyContext(
    dllPath,
    basePathRestriction: pluginDir,
    securityPolicy: AssemblySecurityPolicy.Strict);
```

#### Type Scanning API Change

**Before (v1.0.x):**
```csharp
var types = AssemblyContext.GetLoadedTypes<IPlugin>();
```

**After (v2.0.0):**
```csharp
var types = AssemblyScanner.GetLoadedTypes<IPlugin>();
```

#### Using Path Validation (New Feature)

**Recommended pattern:**
```csharp
// Validate and sanitize user input before using
string userPath = GetPathFromUser();

try
{
    string validatedPath = AssemblyPathValidator.ValidateAndSanitize(
        userPath,
        resolveRelativeToBase: true);
    
    using var context = new AssemblyContext(
        validatedPath,
        basePathRestriction: Path.GetDirectoryName(validatedPath),
        securityPolicy: AssemblySecurityPolicy.Strict);
    
    var plugin = context.CreateInstance<IPlugin>("MyPlugin");
}
catch (ArgumentException ex)
{
    logger.LogError(ex, "Invalid assembly path: {Path}", userPath);
}
```

#### Using Integrity Verification (New Feature)

**Development (learning mode):**
```csharp
var verifier = new AssemblyIntegrityVerifier(
    enabled: true,
    learningMode: true);

using var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    integrityVerifier: verifier);

// Save learned hashes
verifier.HashStore.SaveToFile("trusted-hashes.csv");
```

**Production (strict mode):**
```csharp
var store = new AssemblyHashStore();
store.LoadFromFile("trusted-hashes.csv");

var verifier = new AssemblyIntegrityVerifier(
    enabled: true,
    learningMode: false,
    hashStore: store);

using var context = new AssemblyContext(
    pluginPath,
    basePathRestriction: pluginDir,
    integrityVerifier: verifier);
// Throws SecurityException if hash doesn't match!
```

#### Benefits of Migration
- **Parallel Testing**: No global state interference
- **Per-Context Policies**: Different security levels for trusted vs untrusted plugins
- **Thread Safety**: No race conditions on static configuration
- **Testability**: Easier to test with isolated instances
- **Integrity Protection**: Optional cryptographic verification against tampering
- **Better Errors**: Specific exception types with clear messages
- **Input Validation**: Utilities for sanitizing user-provided paths

#### Custom Security Policies

**New capability:**
```csharp
// Define custom forbidden directories
var customPolicy = new AssemblySecurityPolicy(
    new[] { "temp", "downloads", "desktop" });

var context = new AssemblyContext(
    dllPath,
    basePathRestriction: pluginDir,
    securityPolicy: customPolicy);
```

### Security
- Enhanced path validation with instance-based forbidden directory lists
- Comprehensive audit events for all security-related operations
- Explicit security warnings in documentation and IntelliSense
- Defense-in-depth examples in readme
- Optional cryptographic integrity verification
- Input sanitization utilities
- Professional security guidance throughout documentation

---

## Notes

### SSEM Compliance

This release achieves significant improvements in SSEM (Securable Software Engineering Model) compliance:

**Maintainability**: 7.0 ? 8.5 (+1.5)
- Eliminated static mutable state
- Improved testability with instance-based configuration
- Better exception messages with specific context
- Clean separation of concerns (AssemblyScanner, AssemblyPathValidator)
- Comprehensive professional documentation

**Trustworthiness**: 9.0 ? 9.5 (+0.5)
- Enhanced security documentation
- Comprehensive audit events
- Per-instance security policies
- Optional cryptographic integrity verification

**Reliability**: 8.0 ? 9.0 (+1.0)
- No silent failures
- Specific exception handling
- Clear error propagation
- Input validation utilities

**Overall**: 8.0 ? 8.9 (+0.9 / +11%) - **Approaching "Excellent"**

### Implementation Statistics

**Production Code:**
- 6 new classes created (~1,100 lines)
- 2 classes significantly enhanced
- 9 comprehensive documentation files

**New Classes:**
1. `AssemblySecurityPolicy` - Security policy configuration (85 lines)
2. `AssemblyHashStore` - Hash storage with CSV persistence (244 lines)
3. `AssemblyIntegrityVerifier` - Integrity verification engine (235 lines)
4. `AssemblyScanner` - Type discovery utilities (115 lines)
5. `AssemblyPathValidator` - Path validation utilities (225 lines)

**Test Coverage:**
- 110+ tests total (98 new, 12+ updated)
- ~90% coverage of new code
- Comprehensive edge case testing

**Test Breakdown:**
- Integrity Verification: 62 tests
- Path Validator: 28 tests
- Security: 8+ tests (updated)

**Documentation:**
- CHANGELOG.md (updated with all changes)
- MIGRATION-v1-to-v2.md (12-section comprehensive guide)
- trust-001-implementation-summary.md (implementation details)
- trust-001-test-implementation-summary.md (test documentation)
- phase1-final-summary.md (Phase 1 completion report)
- v2.0-complete-summary.md (complete implementation summary)
- docs/README.md (documentation index)
- Enhanced XML documentation throughout

**Architecture Improvements:**
- Zero static mutable state (thread-safe by design)
- Defense-in-depth security (5 layers)
- Event-based audit trail (complete transparency)
- Optional advanced features (integrity verification)
- Professional documentation standards

### Breaking Changes Impact Assessment

**High Impact (API Changes):**
- Security configuration: Static ? Instance-based
- Affects: All AssemblyContext instantiation code
- Migration effort: Low (simple parameter addition)
- Benefit: Thread safety, parallel testing, per-instance policies

**Medium Impact (Behavior Changes):**
- Exception propagation: Silent failures ? Explicit throws
- Affects: Error handling code
- Migration effort: Low (add try-catch if needed)
- Benefit: Better error visibility, easier debugging

**Low Impact (Method Movement):**
- Type scanning: AssemblyContext.GetLoadedTypes ? AssemblyScanner.GetLoadedTypes
- Affects: Type discovery code
- Migration effort: Very low (simple namespace change)
- Benefit: Better organization, separation of concerns

**Timeline:**
- v2.0.0: Methods deprecated with compiler warnings
- Until 2026-06-01: Full backward compatibility maintained
- v3.0.0: Deprecated methods removed

### Complete Documentation

**For Users:**
- README.md - Updated with security best practices
- MIGRATION-v1-to-v2.md - 12-section migration guide
- CHANGELOG.md - This file

**For Developers:**
- trust-001-implementation-summary.md - Architecture and design
- trust-001-test-implementation-summary.md - Test strategy
- v2.0-complete-summary.md - Complete implementation details
- docs/README.md - Documentation navigation

**For Support:**
- FAQ section in migration guide
- Troubleshooting section with common issues
- Support policy and timeline
- Breaking changes clearly documented

---

## Credits

**SSEM Architecture Review**: Conducted 2025-11-29  
**Implementation**: Phase 1 & Phase 2 complete (100%)

**Phase 1 (Critical Security & Reliability):**
- REL-001: Fix silent failures
- REL-002: Specific exception handling
- MAINT-003: Instance-based security
- DOC-002: Security documentation
- TRUST-001: Integrity verification

**Phase 2 (Maintainability & Documentation):**
- MAINT-004: AssemblyScanner extraction
- DOC-001: Enhanced event documentation
- API-001: AssemblyPathValidator utilities

**Contributors:**
- Architecture improvements based on SSEM framework
- Security enhancements following FIASSE principles
- Exception handling aligned with Microsoft best practices
- Documentation following professional standards
