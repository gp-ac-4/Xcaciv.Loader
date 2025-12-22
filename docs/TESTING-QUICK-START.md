# Dynamic Assembly Monitoring - Testing Infrastructure

## Quick Start

### Run All Tests
```powershell
cd src/Xcaciv.LoaderTests
dotnet test
```

### Run Specific Test Suite
```powershell
# Test DisallowDynamicAssemblies functionality
dotnet test --filter "ClassName=DisallowDynamicAssembliesTests"

# Test GlobalDynamicAssemblyMonitoring functionality  
dotnet test --filter "ClassName=GlobalDynamicAssemblyMonitoringTests"
```

### Run Specific Test
```powershell
dotnet test --filter "Name~EnableGlobalDynamicAssemblyMonitoring_WithStrictPolicy_RaisesSecurityViolation"
```

## Project Organization

### Test Assemblies

#### zTestRiskyAssembly
Demonstrates Reflection.Emit usage and dynamic type creation:
```csharp
// Key behavior: Creates dynamic types using AssemblyBuilder
var risky = new zTestRiskyAssembly.DynamicTypeCreator();
string result = risky.Stuff("input"); // Creates AssemblyBuilder dynamically
```

**Detected By**:
- AssemblyPreflightAnalyzer (static analysis)
- GlobalDynamicAssemblyMonitoring (runtime monitoring)

#### zTestLinqExpressions
Demonstrates LINQ.Expressions.Compile usage:
```csharp
// Key behavior: Compiles expression trees at runtime
var compiler = new zTestLinqExpressions.ExpressionCompiler();
string result = compiler.Stuff("input"); // Compiles expressions dynamically
```

**Detected By**:
- AssemblyPreflightAnalyzer (static analysis)
- GlobalDynamicAssemblyMonitoring (runtime monitoring)

### Test Suites

#### DisallowDynamicAssembliesTests
Tests the `DisallowDynamicAssemblies` security policy property:
- Policy configuration validation
- Context configuration with different policies
- Multi-context isolation
- Property immutability
- Policy inheritance

#### GlobalDynamicAssemblyMonitoringTests  
Tests the `EnableGlobalDynamicAssemblyMonitoring()` method:
- Basic monitoring functionality
- Strict vs Default policy behavior
- Multi-context event delivery
- Thread-safety
- Weak reference cleanup
- Integration with risky assemblies

## Test Execution Workflow

### 1. Security Policy Tests
```csharp
// Verify Strict policy blocks dynamics
var strictPolicy = AssemblySecurityPolicy.Strict;
Assert.True(strictPolicy.DisallowDynamicAssemblies);

// Verify Default policy allows dynamics
var defaultPolicy = AssemblySecurityPolicy.Default;
Assert.False(defaultPolicy.DisallowDynamicAssemblies);
```

### 2. Runtime Monitoring Tests
```csharp
// Create context with Strict policy
using var context = new AssemblyContext(
    path, 
    basePathRestriction: "*",
    securityPolicy: AssemblySecurityPolicy.Strict);

// Enable global dynamic assembly monitoring
context.EnableGlobalDynamicAssemblyMonitoring();

// Subscribe to security violations
bool violated = false;
context.SecurityViolation += (id, msg) => { violated = true; };

// Create dynamic assembly
var assemblyName = new AssemblyName("DynamicAssembly");
AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

// Verify violation was raised
Assert.True(violated);
```

### 3. Integration Tests  
```csharp
// Test with actual risky assembly
var risky = new zTestRiskyAssembly.DynamicTypeCreator();
var result = risky.Stuff("test"); // Creates dynamic type

// Context with monitoring will detect it
// Verifies end-to-end workflow
```

## Test Coverage

| Feature | Tests | Status |
|---------|-------|--------|
| DisallowDynamicAssemblies property | 12 | ? |
| EnableGlobalDynamicAssemblyMonitoring | 10 | ? |
| AssemblyPreflightAnalyzer integration | 2 | ? |
| **Total** | **24+** | ? |

## Key Test Scenarios

### Scenario 1: Static Detection with Preflight Analysis
```
Input: zTestRiskyAssembly.dll
Process: AssemblyPreflightAnalyzer.Analyze()
Output: HasAnyIndicators = true
Details: Detects Reflection.Emit usage
Status: ? Works without loading assembly
```

### Scenario 2: Runtime Monitoring with Strict Policy
```
Setup: AssemblyContext with Strict policy
Enable: GlobalDynamicAssemblyMonitoring()
Action: Execute zTestRiskyAssembly code
Result: SecurityViolation event raised
Status: ? Detects dynamic assembly creation
```

### Scenario 3: Silent Operation with Default Policy
```
Setup: AssemblyContext with Default policy
Enable: GlobalDynamicAssemblyMonitoring()
Action: Create dynamic assembly with AssemblyBuilder
Result: No SecurityViolation event
Status: ? Correct policy-based behavior
```

### Scenario 4: Thread-Safe Multi-Context Monitoring
```
Setup: 10 contexts with Strict policy
Action: All call EnableGlobalDynamicAssemblyMonitoring() concurrently
Create: 1 dynamic assembly
Result: All 10 contexts receive SecurityViolation event
Status: ? Thread-safe, no duplicates
```

### Scenario 5: Memory-Safe Cleanup
```
Setup: Create context, enable monitoring, dispose
GC: Force garbage collection
Create: New dynamic assembly
Result: Disposed context doesn't receive event
Status: ? Weak references prevent memory leaks
```

## Verification Steps

### 1. Build Verification
```powershell
dotnet build
# Expected: Build successful, all projects compile
```

### 2. Test Execution
```powershell
dotnet test --logger "console;verbosity=detailed"
# Expected: 204+ tests, all passing
```

### 3. Specific Feature Tests
```powershell
# Test DisallowDynamicAssemblies
dotnet test --filter "ClassName=DisallowDynamicAssembliesTests"
# Expected: 12 tests passing

# Test GlobalDynamicAssemblyMonitoring  
dotnet test --filter "ClassName=GlobalDynamicAssemblyMonitoringTests"
# Expected: 10+ tests passing
```

### 4. Coverage Report
```powershell
dotnet test /p:CollectCoverage=true
# Expected: >90% coverage for security features
```

## Troubleshooting

### Tests Timing Out
**Issue**: Test takes longer than expected to complete

**Solution**: 
- Weak reference cleanup may be slow if GC hasn't run
- Consider running with GC.Collect() before assertions
- Check system resources

### Memory Leak Detected
**Issue**: Test reports memory usage increasing

**Solution**:
- Dispose contexts properly in finally blocks
- Force GC.WaitForPendingFinalizers() after dispose
- Verify weak references are being cleaned

### Event Not Raised
**Issue**: SecurityViolation event not firing

**Cause**: Check these in order
1. Policy has DisallowDynamicAssemblies = true?
2. EnableGlobalDynamicAssemblyMonitoring() was called?
3. Dynamic assembly actually created (not loaded)?
4. Sufficient time for event handler execution?

**Solution**:
```csharp
// Verify policy
Assert.True(context.SecurityPolicy.DisallowDynamicAssemblies);

// Verify subscription
context.EnableGlobalDynamicAssemblyMonitoring();

// Verify wait time
Thread.Sleep(100); // Give event handler time to run

// Verify dynamic assembly
var asm = AssemblyBuilder.DefineDynamicAssembly(...);
Assert.True(asm.IsDynamic); // Confirm it's dynamic
```

## Best Practices

### Writing New Tests
1. **Arrange-Act-Assert**: Clear test structure
2. **Resource Cleanup**: Use try/finally or using statements
3. **Meaningful Names**: Test name describes what's verified
4. **Isolated Tests**: Don't depend on execution order
5. **Clear Assertions**: Specific error messages

### Test Assembly Usage
1. **zTestRiskyAssembly**: Use for Reflection.Emit scenarios
2. **zTestLinqExpressions**: Use for expression compilation scenarios
3. **Never Deploy**: These are test-only assemblies
4. **Document Intent**: Comments explaining why assembly is risky

## Integration with CI/CD

### GitHub Actions Example
```yaml
- name: Run Security Tests
  run: |
    dotnet test src/Xcaciv.LoaderTests/Xcaciv.LoaderTests.csproj \
      --filter "ClassName=DisallowDynamicAssembliesTests or ClassName=GlobalDynamicAssemblyMonitoringTests" \
      --logger "trx" \
      --collect:"XPlat Code Coverage"
```

### Build Pipeline Steps
1. Build solution
2. Run full test suite
3. Generate coverage report
4. Verify >90% coverage on security features
5. Publish test results
6. Validate no security warnings

## Documentation References

- **TESTING-DYNAMIC-ASSEMBLY-MONITORING.md** - Comprehensive testing guide
- **IMPLEMENTATION-SUMMARY.md** - Architecture and design details
- **TEST-COVERAGE-VERIFICATION.md** - Coverage checklist
- **CHANGELOG.md** - Version history and features

## Support

For issues or questions:
1. Check test names for hints about specific features
2. Review test implementation in test files
3. Examine test assemblies (zTestRisky*, zTestLinq*)
4. Run tests with verbose logging
5. Check security event messages

## Version Compatibility

- **Framework**: .NET 8.0+
- **Testing Framework**: xUnit
- **Supported Platforms**: Windows, Linux, macOS
- **Build Tool**: dotnet CLI

## Performance Expectations

- **Full Test Suite**: < 5 seconds
- **Single Test Class**: < 1 second  
- **Integration Tests**: < 100ms per test
- **Memory Usage**: < 500MB for full suite

## Future Enhancements

- [ ] Performance benchmarking tests
- [ ] Stress testing with many dynamic assemblies
- [ ] Cross-domain testing (if applicable)
- [ ] Load testing integration
- [ ] Snapshot testing for policy configurations
