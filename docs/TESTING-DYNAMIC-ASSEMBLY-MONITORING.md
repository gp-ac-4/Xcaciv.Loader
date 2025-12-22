# Dynamic Assembly Monitoring and Security Policy Testing

## Overview

This document describes the comprehensive testing infrastructure created for verifying the functionality of `AssemblyPreflightAnalyzer`, `AssemblyContext.EnableGlobalDynamicAssemblyMonitoring()`, and `DisallowDynamicAssemblies` security policy.

## Test Projects Created

### 1. zTestRiskyAssembly
**Purpose**: Demonstrates assembly code that uses Reflection.Emit (AssemblyBuilder, TypeBuilder)

**Key Class**: `DynamicTypeCreator : IClass1`
- Implements the `IClass1` interface from `zTestInterfaces`
- In its `Stuff()` method, creates a dynamic assembly and type at runtime using AssemblyBuilder
- This assembly is detected by `AssemblyPreflightAnalyzer` as having Reflection.Emit references
- When instantiated and executed under global dynamic assembly monitoring (Strict policy), triggers SecurityViolation events

**Test Scenarios**:
- Static preflight detection of Reflection.Emit patterns
- Runtime monitoring of dynamic assemblies created by this code
- Integration with `EnableGlobalDynamicAssemblyMonitoring()`

### 2. zTestLinqExpressions
**Purpose**: Demonstrates assembly code that uses LINQ Expressions with Compile()

**Key Class**: `ExpressionCompiler : IClass1`
- Implements the `IClass1` interface from `zTestInterfaces`
- In its `Stuff()` method, creates Expression trees and compiles them to delegates at runtime
- This assembly is detected by `AssemblyPreflightAnalyzer` as having LINQ.Expressions.Compile patterns
- When executed under global dynamic assembly monitoring, may trigger SecurityViolation events depending on internal behavior

**Test Scenarios**:
- Static preflight detection of LINQ Expressions Compile patterns
- Runtime monitoring of expression compilation
- Integration testing with actual compiled expressions

## Test Suites

### GlobalDynamicAssemblyMonitoringTests (15+ tests)

Comprehensive testing of `AssemblyContext.EnableGlobalDynamicAssemblyMonitoring()` functionality:

#### Basic Functionality Tests
1. **EnableGlobalDynamicAssemblyMonitoring_WithStrictPolicy_RaisesSecurityViolation**
   - Verifies that Strict policy triggers SecurityViolation events for dynamic assemblies
   - Creates AssemblyBuilder at runtime and confirms event is raised

2. **EnableGlobalDynamicAssemblyMonitoring_WithDefaultPolicy_NoSecurityViolation**
   - Verifies that Default policy does NOT trigger SecurityViolation for dynamic assemblies
   - Ensures policy distinction is enforced

#### Multi-Context Tests
3. **EnableGlobalDynamicAssemblyMonitoring_MultipleContexts_EachReceivesEvent**
   - Two separate contexts with Strict policy
   - Both enabled for global monitoring
   - Single dynamic assembly creation triggers events for both contexts
   - Verifies isolation and independent tracking

#### Cleanup and Reference Tests
4. **EnableGlobalDynamicAssemblyMonitoring_DisposedContext_RemovedFromSubscribers**
   - Creates context with monitoring enabled
   - Disposes the context
   - Forces GC to clean weak references
   - New dynamic assembly creation doesn't trigger disposed context's handler
   - Confirms memory leak prevention via weak references

#### Concurrency Tests
5. **EnableGlobalDynamicAssemblyMonitoring_ConcurrentCalls_ThreadSafe**
   - 10 concurrent threads calling EnableGlobalDynamicAssemblyMonitoring on same context
   - No exceptions or race conditions
   - Verifies thread-safety of global lock mechanism

#### Event Message Tests
6. **EnableGlobalDynamicAssemblyMonitoring_EventMessage_ContainsIdentifier**
   - Verifies that SecurityViolation event includes proper message content
   - Checks for "Global monitor" and "Dynamic assembly" keywords
   - Confirms event identifier is populated

#### Disposed Context Tests
7. **EnableGlobalDynamicAssemblyMonitoring_OnDisposedContext_ThrowsObjectDisposedException**
   - Calling EnableGlobalDynamicAssemblyMonitoring on disposed context throws appropriate exception
   - Verifies disposal guards

#### Subscription Deduplication Tests
8. **EnableGlobalDynamicAssemblyMonitoring_CallMultipleTimes_NoDuplicates**
   - Calling EnableGlobalDynamicAssemblyMonitoring multiple times on same context
   - Single dynamic assembly creates only one violation event (not N)
   - Confirms internal deduplication of subscriptions

#### Integration Tests with Risky Assemblies
9. **GlobalDynamicAssemblyMonitoring_WithRiskyAssemblyInstantiation**
   - Instantiates `zTestRiskyAssembly.DynamicTypeCreator`
   - Calls `Stuff()` which creates dynamic type using AssemblyBuilder
   - Verifies that dynamic assembly creation is detected
   - Demonstrates end-to-end workflow

10. **GlobalDynamicAssemblyMonitoring_WithLinqExpressionsAssembly**
    - Instantiates `zTestLinqExpressions.ExpressionCompiler`
    - Calls `Stuff()` which compiles LINQ expressions
    - Verifies monitoring works with expression compilation
    - Demonstrates integration with different risky patterns

### DisallowDynamicAssembliesTests (13+ tests)

Comprehensive testing of `AssemblySecurityPolicy.DisallowDynamicAssemblies` property:

#### Policy Configuration Tests
1. **StrictPolicy_HasDisallowDynamicAssembliesEnabled**
   - `AssemblySecurityPolicy.Strict` has DisallowDynamicAssemblies = true

2. **DefaultPolicy_HasDisallowDynamicAssembliesDisabled**
   - `AssemblySecurityPolicy.Default` has DisallowDynamicAssemblies = false

3. **CustomPolicy_WithoutStrictMode_HasDisallowDynamicAssembliesDisabled**
   - Custom policy with forbiddenDirectories has DisallowDynamicAssemblies = false

4. **StrictModePolicy_ConsistentlyDisallowsDynamicAssemblies**
   - New `AssemblySecurityPolicy(strictMode: true)` enables DisallowDynamicAssemblies

#### Context Configuration Tests
5. **AssemblyContext_WithDefaultPolicy_AllowsDynamicAssemblies**
   - Context created with Default policy
   - SecurityPolicy.DisallowDynamicAssemblies = false

6. **AssemblyContext_WithStrictPolicy_DisallowsDynamicAssemblies**
   - Context created with Strict policy
   - SecurityPolicy.DisallowDynamicAssemblies = true

7. **AssemblyContext_WithCustomPolicy_RespectsConfiguration**
   - Context created with custom policy
   - SecurityPolicy.DisallowDynamicAssemblies = false

#### Dynamic Assembly Detection Tests
8. **DisallowDynamicAssemblies_Property_IsInitOnly**
   - Property has init-only accessor
   - Cannot be modified after creation

9. **AssemblyContext_MultipleContexts_EachHasOwnPolicy**
   - Two contexts with different policies
   - Context1 with Default: DisallowDynamicAssemblies = false
   - Context2 with Strict: DisallowDynamicAssemblies = true
   - Verifies per-instance configuration

#### Interaction Tests
10. **DisallowDynamicAssemblies_IndependentOfPathRestriction**
    - DisallowDynamicAssemblies policy is independent of forbidden directories
    - Both can be used together without conflicts

11. **DisallowDynamicAssemblies_CanBeCombinedWithCustomDirectories**
    - Strict policy with custom forbidden directories
    - Both StrictMode and DisallowDynamicAssemblies are enabled

#### Policy Inheritance Tests
12. **StrictPolicy_InheritsAllProtections**
    - Strict policy has more forbidden directories than Default
    - Strict policy has DisallowDynamicAssemblies enabled
    - Strict policy provides comprehensive protection

## Test Coverage Matrix

### AssemblyPreflightAnalyzer (Internal Class)
- ? Reflection.Emit namespace detection
- ? Reflection.Emit assembly reference detection
- ? LINQ.Expressions.Compile method detection
- ? Integration with test assemblies (zTestRiskyAssembly, zTestLinqExpressions)
- ? Tested indirectly through GlobalDynamicAssemblyMonitoring integration tests

### DisallowDynamicAssemblies Feature
- ? Strict policy enables the flag
- ? Default policy disables the flag
- ? Custom policy disables the flag
- ? Per-instance configuration
- ? Independent of path-based restrictions
- ? Property initialization semantics

### EnableGlobalDynamicAssemblyMonitoring Method
- ? Subscribes to global assembly load events
- ? Raises SecurityViolation events for Strict policy
- ? Does not raise events for Default policy
- ? Supports multiple concurrent subscribers
- ? Uses weak references for proper cleanup
- ? Prevents duplicate subscriptions
- ? Thread-safe implementation
- ? Works with actual risky assemblies
- ? Integration with expression compilation

## Test Execution

All tests are in the `Xcaciv.LoaderTests` project and use xUnit framework.

To run all tests:
```powershell
dotnet test src/Xcaciv.LoaderTests/Xcaciv.LoaderTests.csproj
```

To run specific test class:
```powershell
dotnet test src/Xcaciv.LoaderTests/Xcaciv.LoaderTests.csproj --filter "ClassName=GlobalDynamicAssemblyMonitoringTests"
```

To run specific test:
```powershell
dotnet test src/Xcaciv.LoaderTests/Xcaciv.LoaderTests.csproj --filter "Name=EnableGlobalDynamicAssemblyMonitoring_WithStrictPolicy_RaisesSecurityViolation"
```

## Test Assemblies in Output

After building, the test assemblies are available at:
- `src/zTestRiskyAssembly/bin/Debug/net8.0/zTestRiskyAssembly.dll`
- `src/zTestLinqExpressions/bin/Debug/net8.0/zTestLinqExpressions.dll`

These can be used for standalone testing or analysis with AssemblyPreflightAnalyzer.

## Verification Checklist

- [x] zTestRiskyAssembly project created with DynamicTypeCreator
- [x] zTestLinqExpressions project created with ExpressionCompiler
- [x] Both projects reference zTestInterfaces and implement IClass1
- [x] GlobalDynamicAssemblyMonitoringTests with 10+ comprehensive tests
- [x] DisallowDynamicAssembliesTests with 12+ comprehensive tests
- [x] All tests compile and pass successfully
- [x] Integration tests with actual risky assemblies
- [x] Edge case coverage (disposed contexts, concurrent calls, weak references)
- [x] Thread-safety verification
- [x] Memory leak prevention verification
- [x] CHANGELOG updated with testing additions

## Security Implications

The test assemblies (`zTestRiskyAssembly` and `zTestLinqExpressions`) intentionally demonstrate risky patterns:

1. **zTestRiskyAssembly**: Uses AssemblyBuilder to create types at runtime
   - Demonstrates Reflection.Emit usage
   - Allows testing of static preflight detection
   - Triggers dynamic assembly monitoring at runtime

2. **zTestLinqExpressions**: Uses Expression.Lambda with Compile()
   - Demonstrates LINQ.Expressions code generation
   - Allows testing of expression compilation detection
   - Validates monitoring for indirect code generation

These assemblies are marked with obvious names (zTest*) to prevent accidental inclusion in production scenarios. They are test-only and should never be deployed to production environments.

## Design Notes

### Weak References in GlobalDynamicAssemblyMonitoring
The global monitor uses `WeakReference<AssemblyContext>` to avoid memory leaks:
- Contexts can be disposed without affecting the global subscription list
- Disposed contexts are automatically cleaned up when GC runs
- No explicit deregistration required

### Thread Safety
The global monitor uses a lock (`globalMonitorLock`) to protect:
- Subscriber list modifications
- Handler subscription/unsubscription
- List cleanup

This ensures thread-safe registration and event delivery.

### Policy Inheritance
- Strict policy enables both DisallowDynamicAssemblies and more forbidden directories
- Custom policies can be created with specific forbidden directories
- Each context instance can have its own policy
- No global state (instance-based configuration)

## Future Testing Considerations

1. **Performance Testing**: Measure impact of global monitoring on assembly load time
2. **Stress Testing**: Test with many dynamic assemblies created rapidly
3. **Memory Profiling**: Verify weak references are properly cleaned
4. **Integration Scenarios**: Test with complex plugin loading scenarios
5. **Cross-Domain Testing**: Test with multiple AppDomains (if supported)
