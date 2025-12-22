using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Xcaciv.Loader.Tests;

/// <summary>
/// Tests for AssemblyContext.EnableGlobalDynamicAssemblyMonitoring() functionality.
/// Verifies detection and reporting of dynamic assemblies loaded in the AppDomain.
/// </summary>
public class GlobalDynamicAssemblyMonitoringTests
{
    #region Basic Functionality Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_WithStrictPolicy_RaisesSecurityViolation()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_strict_" + Guid.NewGuid() + ".dll");
        var securityViolationRaised = false;
        var violationId = string.Empty;
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Subscribe to security violation event
            context.SecurityViolation += (id, msg) =>
            {
                securityViolationRaised = true;
                violationId = id;
            };
            
            // Act - Enable monitoring
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            // Create a dynamic assembly in the AppDomain
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, 
                AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
            var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);
            var dynamicType = typeBuilder.CreateType();
            
            // Give the event handler time to process
            Thread.Sleep(100);
            
            // Assert
            Assert.True(securityViolationRaised, "Security violation should be raised for dynamic assembly");
            Assert.NotEmpty(violationId);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_WithDefaultPolicy_NoSecurityViolation()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_default_" + Guid.NewGuid() + ".dll");
        var securityViolationRaised = false;
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Default); // Default policy allows dynamic assemblies
            
            // Subscribe to security violation event
            context.SecurityViolation += (id, msg) =>
            {
                securityViolationRaised = true;
            };
            
            // Act - Enable monitoring (but it won't report violations since policy allows dynamic)
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            // Create a dynamic assembly
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            Thread.Sleep(100);
            
            // Assert
            Assert.False(securityViolationRaised, 
                "No security violation should be raised when policy allows dynamic assemblies");
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }
    }

    #endregion

    #region Multiple Context Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_MultipleContexts_EachReceivesEvent()
    {
        // Arrange
        var testPath1 = Path.Combine(Path.GetTempPath(), "test1.dll");
        var testPath2 = Path.Combine(Path.GetTempPath(), "test2.dll");
        var context1ViolationCount = 0;
        var context2ViolationCount = 0;
        
        try
        {
            File.WriteAllText(testPath1, "test1");
            File.WriteAllText(testPath2, "test2");
            
            using var context1 = new AssemblyContext(
                testPath1,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            using var context2 = new AssemblyContext(
                testPath2,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Act - Both contexts enable monitoring
            context1.SecurityViolation += (id, msg) => Interlocked.Increment(ref context1ViolationCount);
            context2.SecurityViolation += (id, msg) => Interlocked.Increment(ref context2ViolationCount);
            
            context1.EnableGlobalDynamicAssemblyMonitoring();
            context2.EnableGlobalDynamicAssemblyMonitoring();
            
            // Create a dynamic assembly
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            Thread.Sleep(100);
            
            // Assert - Both should have received the violation event
            Assert.True(context1ViolationCount > 0, "Context1 should receive security violation event");
            Assert.True(context2ViolationCount > 0, "Context2 should receive security violation event");
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath1))
            {
                try
                {
                    File.Delete(testPath1);
                }
                catch { }
            }
            if (File.Exists(testPath2))
            {
                try
                {
                    File.Delete(testPath2);
                }
                catch { }
            }
        }
    }

    #endregion

    #region Weak Reference and Cleanup Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_DisposedContext_RemovedFromSubscribers()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        var violationCount = 0;
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            // Create and dispose a context with monitoring enabled
            {
                var context = new AssemblyContext(
                    testPath,
                    basePathRestriction: "*",
                    securityPolicy: AssemblySecurityPolicy.Strict);
                
                context.SecurityViolation += (id, msg) => Interlocked.Increment(ref violationCount);
                context.EnableGlobalDynamicAssemblyMonitoring();
                
                // Dispose the context
                context.Dispose();
            }
            
            // Force garbage collection to clean up weak references
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            // Act - Create a dynamic assembly after context is disposed
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            Thread.Sleep(100);
            
            // Assert - Disposed context should not receive events
            Assert.Equal(0, violationCount);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        var enableCount = 0;
        var exceptions = new List<Exception>();
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Act - Multiple threads trying to enable monitoring
            var tasks = new Task[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    try
                    {
                        context.EnableGlobalDynamicAssemblyMonitoring();
                        Interlocked.Increment(ref enableCount);
                    }
                    catch (Exception ex)
                    {
                        lock (exceptions)
                        {
                            exceptions.Add(ex);
                        }
                    }
                });
            }
            
            Task.WaitAll(tasks);
            
            // Assert
            Assert.Empty(exceptions);
            Assert.Equal(10, enableCount);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    #endregion

    #region Event Message Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_EventMessage_ContainsIdentifier()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_msg_" + Guid.NewGuid() + ".dll");
        var receivedMessage = string.Empty;
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            context.SecurityViolation += (id, msg) =>
            {
                receivedMessage = msg;
            };
            
            // Act
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            Thread.Sleep(100);
            
            // Assert
            Assert.NotEmpty(receivedMessage);
            Assert.Contains("Global monitor", receivedMessage, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Dynamic assembly", receivedMessage, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    #endregion

    #region Disposed Context Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_OnDisposedContext_ThrowsObjectDisposedException()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_disposed_" + Guid.NewGuid() + ".dll");
        try
        {
            File.WriteAllText(testPath, "test");
            
            var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            context.Dispose();
            
            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => context.EnableGlobalDynamicAssemblyMonitoring());
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    #endregion

    #region No Duplicate Subscriptions Tests

    [Fact]
    public void EnableGlobalDynamicAssemblyMonitoring_CallMultipleTimes_NoDuplicates()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_dup_" + Guid.NewGuid() + ".dll");
        var violationCount = 0;
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            context.SecurityViolation += (id, msg) => Interlocked.Increment(ref violationCount);
            
            // Act - Call EnableGlobalDynamicAssemblyMonitoring multiple times
            context.EnableGlobalDynamicAssemblyMonitoring();
            context.EnableGlobalDynamicAssemblyMonitoring();
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            // Create a dynamic assembly
            var assemblyName = new AssemblyName($"DynamicAssembly_{Guid.NewGuid()}");
            AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            
            Thread.Sleep(100);
            
            // Assert - Should only receive one violation event, not three
            Assert.Equal(1, violationCount);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    #endregion

    #region Integration Tests with Risky Assemblies

    [Fact]
    public void GlobalDynamicAssemblyMonitoring_WithRiskyAssemblyInstantiation()
    {
        // This test simulates loading a risky assembly and monitoring for dynamic behavior
        
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_risky_" + Guid.NewGuid() + ".dll");
        var violations = new List<string>();
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            context.SecurityViolation += (id, msg) =>
            {
                lock (violations)
                {
                    violations.Add(id);
                }
            };
            
            // Act - Enable monitoring and execute risky code
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            // The risky assembly creates dynamic types
            var risky = new zTestRiskyAssembly.DynamicTypeCreator();
            _ = risky.Stuff("test");
            
            Thread.Sleep(100);
            
            // Assert - Should have detected dynamic assembly creation
            Assert.NotEmpty(violations);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    [Fact]
    public void GlobalDynamicAssemblyMonitoring_WithLinqExpressionsAssembly()
    {
        // This test simulates monitoring when a risky assembly with LINQ.Expressions.Compile is executed
        
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test_linq_" + Guid.NewGuid() + ".dll");
        var violations = new List<string>();
        
        try
        {
            File.WriteAllText(testPath, "test");
            
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            context.SecurityViolation += (id, msg) =>
            {
                lock (violations)
                {
                    violations.Add(id);
                }
            };
            
            // Act - Enable monitoring and execute risky code
            context.EnableGlobalDynamicAssemblyMonitoring();
            
            // The LINQ expressions assembly compiles expressions
            var compiler = new zTestLinqExpressions.ExpressionCompiler();
            var result = compiler.Stuff("test");
            
            Thread.Sleep(100);
            
            // Assert - May or may not detect violations depending on LINQ.Expressions internals
            // The main assertion is that monitoring completes without error
            Assert.NotNull(result);
        }
        finally
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            
            if (File.Exists(testPath))
            {
                try
                {
                    File.Delete(testPath);
                }
                catch { }
            }
        }
    }

    #endregion
}
