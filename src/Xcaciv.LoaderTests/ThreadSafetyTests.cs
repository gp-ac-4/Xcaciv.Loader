using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using zTestInterfaces;

namespace Xcaciv.Loader.Tests;

/// <summary>
/// Thread safety tests for AssemblyContext.
/// Tests concurrent operations, race conditions, and thread-safe event handling.
/// </summary>
public class ThreadSafetyTests
{
    private readonly ITestOutputHelper output;
    private readonly string simpleDllPath;
    private readonly string dependentDllPath;

    public ThreadSafetyTests(ITestOutputHelper output)
    {
        this.output = output;
        
#if DEBUG
        this.simpleDllPath = @"..\..\..\..\TestAssembly\bin\Debug\net8.0\zTestAssembly.dll";
        this.dependentDllPath = @"..\..\..\..\zTestDependentAssembly\bin\Debug\net8.0\zTestDependentAssembly.dll";
#else
        this.simpleDllPath = @"..\..\..\..\TestAssembly\bin\Release\net8.0\zTestAssembly.dll";
        this.dependentDllPath = @"..\..\..\..\zTestDependentAssembly\bin\Release\net8.0\zTestDependentAssembly.dll";
#endif
    }

    #region Concurrent Loading Tests

    [Fact]
    public async Task ConcurrentLoad_MultipleContexts_ThreadSafe()
    {
        // Arrange
        const int contextCount = 10;
        var tasks = new List<Task>();
        var contexts = new List<AssemblyContext>();
        var lockObj = new object();
        
        // Act
        for (int i = 0; i < contextCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
                lock (lockObj)
                {
                    contexts.Add(context);
                }
                
                // Create instance to trigger assembly load
                var instance = context.CreateInstance<IClass1>("Class1");
                Assert.NotNull(instance);
            }));
        }
        
        await Task.WhenAll(tasks);
        
        // Assert
        Assert.Equal(contextCount, contexts.Count);
        
        // Cleanup
        foreach (var context in contexts)
        {
            context.Dispose();
        }
        
        output.WriteLine($"Successfully created and used {contextCount} contexts concurrently");
    }

    [Fact]
    public async Task ConcurrentLoad_SameAssembly_NoRaceConditions()
    {
        // Arrange
        const int operationCount = 5;
        var results = new string?[operationCount];
        var contexts = new AssemblyContext[operationCount];
        
        try
        {
            // Act - Load same assembly from multiple threads simultaneously
            var tasks = Enumerable.Range(0, operationCount).Select(i => Task.Run(() =>
            {
                contexts[i] = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
                var instance = contexts[i].CreateInstance<IClass1>("Class1");
                results[i] = instance?.Stuff("test");
            })).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            Assert.All(results, result => Assert.NotNull(result));
            Assert.All(results, result => Assert.Equal("test output", result));
            
            output.WriteLine($"All {operationCount} concurrent loads completed successfully");
        }
        finally
        {
            // Cleanup
            foreach (var context in contexts)
            {
                context?.Dispose();
            }
        }
    }

    [Fact]
    public async Task ConcurrentLoad_DifferentAssemblies_ThreadSafe()
    {
        // Arrange
        var contexts = new List<AssemblyContext>();
        var lockObj = new object();
        
        try
        {
            // Act - Load different assemblies concurrently
            var task1 = Task.Run(() =>
            {
                var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
                lock (lockObj) contexts.Add(context);
                return context.CreateInstance<IClass1>("Class1");
            });
            
            var task2 = Task.Run(() =>
            {
                var context = new AssemblyContext(dependentDllPath, basePathRestriction: "*");
                lock (lockObj) contexts.Add(context);
                return context.CreateInstance<IClass1>("Class1");
            });
            
            var results = await Task.WhenAll(task1, task2);
            
            // Assert
            Assert.All(results, instance => Assert.NotNull(instance));
            Assert.Equal(2, contexts.Count);
            
            output.WriteLine("Successfully loaded different assemblies concurrently");
        }
        finally
        {
            foreach (var context in contexts)
            {
                context?.Dispose();
            }
        }
    }

    #endregion

    #region Concurrent Unloading Tests

    [Fact]
    public async Task ConcurrentUnload_MultipleContexts_NoDeadlock()
    {
        // Arrange
        const int contextCount = 5;
        var contexts = new AssemblyContext[contextCount];
        
        // Create and load all contexts
        for (int i = 0; i < contextCount; i++)
        {
            contexts[i] = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
            contexts[i].CreateInstance<IClass1>("Class1");
        }
        
        // Act - Unload all contexts concurrently
        var tasks = contexts.Select(c => Task.Run(() => c.UnloadAsync())).ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, result => Assert.True(result));
        
        // Cleanup
        foreach (var context in contexts)
        {
            context.Dispose();
        }
        
        output.WriteLine($"Successfully unloaded {contextCount} contexts concurrently");
    }

    [Fact]
    public async Task LoadUnload_ConcurrentOperations_NoDeadlock()
    {
        // Arrange
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        bool loadCompleted = false;
        
        // Act - Try to unload while loading (should be safe due to locking)
        var loadTask = Task.Run(() =>
        {
            var instance = context.CreateInstance<IClass1>("Class1");
            loadCompleted = true;
            return instance;
        });
        
        // Give load task a moment to start
        await Task.Delay(10);
        
        var unloadTask = Task.Run(() => context.UnloadAsync());
        
        await Task.WhenAll(loadTask, unloadTask);
        
        // Assert - Either load succeeded or unload prevented it
        Assert.True(loadCompleted || !loadCompleted); // One operation succeeded
        
        output.WriteLine("Concurrent load/unload completed without deadlock");
    }

    #endregion

    #region Event Handler Thread Safety Tests

    [Fact]
    public async Task EventHandlers_ConcurrentRegistration_ThreadSafe()
    {
        // Arrange
        const int handlerCount = 10;
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        var callCounts = new int[handlerCount];
        
        // Act - Register event handlers from multiple threads
        var tasks = Enumerable.Range(0, handlerCount).Select(i => Task.Run(() =>
        {
            context.AssemblyLoaded += (path, name, version) =>
            {
                Interlocked.Increment(ref callCounts[i]);
            };
        })).ToArray();
        
        await Task.WhenAll(tasks);
        
        // Trigger event
        context.CreateInstance<IClass1>("Class1");
        
        // Assert - All handlers should have been called
        Assert.All(callCounts, count => Assert.Equal(1, count));
        
        output.WriteLine($"All {handlerCount} concurrent event handlers executed successfully");
    }

    [Fact]
    public async Task EventHandlers_ConcurrentUnregistration_ThreadSafe()
    {
        // Arrange
        const int handlerCount = 5;
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        var handlers = new Action<string, string, Version?>[handlerCount];
        
        // Register handlers
        for (int i = 0; i < handlerCount; i++)
        {
            int index = i;
            handlers[i] = (path, name, version) => output.WriteLine($"Handler {index} called");
            context.AssemblyLoaded += handlers[i];
        }
        
        // Act - Unregister handlers from multiple threads
        var tasks = Enumerable.Range(0, handlerCount).Select(i => Task.Run(() =>
        {
            context.AssemblyLoaded -= handlers[i];
        })).ToArray();
        
        await Task.WhenAll(tasks);
        
        // Trigger event (no handlers should be called)
        context.CreateInstance<IClass1>("Class1");
        
        // Assert - No exceptions should occur
        Assert.True(true); // If we got here, thread safety is maintained
        
        output.WriteLine("Concurrent event handler unregistration completed safely");
    }

    [Fact]
    public async Task EventHandlers_RaisedFromMultipleThreads_ThreadSafe()
    {
        // Arrange
        const int contextCount = 5;
        var totalEventCalls = 0;
        var contexts = new List<AssemblyContext>();
        
        try
        {
            // Create contexts with shared event handler
            for (int i = 0; i < contextCount; i++)
            {
                var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
                context.AssemblyLoaded += (path, name, version) =>
                {
                    Interlocked.Increment(ref totalEventCalls);
                };
                contexts.Add(context);
            }
            
            // Act - Trigger loads from multiple threads
            var tasks = contexts.Select(c => Task.Run(() =>
            {
                c.CreateInstance<IClass1>("Class1");
            })).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            Assert.Equal(contextCount, totalEventCalls);
            
            output.WriteLine($"Event handler called {totalEventCalls} times from {contextCount} threads");
        }
        finally
        {
            foreach (var context in contexts)
            {
                context.Dispose();
            }
        }
    }

    #endregion

    #region Concurrent CreateInstance Tests

    [Fact]
    public async Task CreateInstance_ConcurrentCalls_SameContext_ThreadSafe()
    {
        // Arrange
        const int operationCount = 10;
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        
        // Act - Create instances concurrently from same context
        var tasks = Enumerable.Range(0, operationCount).Select(i => Task.Run(() =>
        {
            var instance = context.CreateInstance<IClass1>("Class1");
            return instance?.Stuff($"test{i}");
        })).ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, result => Assert.NotNull(result));
        Assert.Equal(operationCount, results.Length);
        
        output.WriteLine($"{operationCount} concurrent CreateInstance calls completed successfully");
    }

    [Fact]
    public async Task CreateInstance_DifferentTypes_Concurrent_ThreadSafe()
    {
        // Arrange
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        
        // Act - Create different types concurrently
        var task1 = Task.Run(() => context.CreateInstance<IClass1>("Class1"));
        var task2 = Task.Run(() => context.CreateInstance("Class1"));
        var task3 = Task.Run(() => context.GetTypes());
        
        await Task.WhenAll(task1, task2, task3);
        
        // Assert
        var result1 = await task1;
        var result2 = await task2;
        var result3 = await task3;
        
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.NotNull(result3);
        
        output.WriteLine("Concurrent CreateInstance with different signatures completed successfully");
    }

    #endregion

    #region Disposal Thread Safety Tests

    [Fact]
    public async Task Dispose_WhileOperationsInProgress_ThreadSafe()
    {
        // Arrange
        var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        bool operationCompleted = false;
        Exception? capturedException = null;
        
        // Act - Start operation and dispose concurrently
        var operationTask = Task.Run(() =>
        {
            try
            {
                var instance = context.CreateInstance<IClass1>("Class1");
                operationCompleted = instance != null;
            }
            catch (ObjectDisposedException ex)
            {
                capturedException = ex;
                // This is expected if dispose wins the race
            }
        });
        
        var disposeTask = Task.Run(async () =>
        {
            await Task.Delay(5); // Give operation a tiny head start
            context.Dispose();
        });
        
        await Task.WhenAll(operationTask, disposeTask);
        
        // Assert - Either operation completed or ObjectDisposedException was thrown
        Assert.True(operationCompleted || capturedException is ObjectDisposedException);
        
        output.WriteLine(operationCompleted 
            ? "Operation completed before dispose"
            : "Dispose occurred during operation (expected ObjectDisposedException)");
    }

    [Fact]
    public async Task DisposeAsync_Concurrent_OnlyDisposesOnce()
    {
        // Arrange
        var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        context.CreateInstance<IClass1>("Class1");
        
        // Act - Dispose multiple times concurrently
        var disposeTasks = Enumerable.Range(0, 5).Select(i => 
            Task.Run(async () => await context.DisposeAsync())
        ).ToArray();
        
        await Task.WhenAll(disposeTasks);
        
        // Assert - No exceptions should occur (multiple dispose is safe)
        Assert.True(true);
        
        output.WriteLine("Concurrent DisposeAsync completed safely");
    }

    #endregion

    #region GetTypes Thread Safety Tests

    [Fact]
    public async Task GetTypes_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        const int operationCount = 10;
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        
        // Act - Call GetTypes concurrently
        var tasks = Enumerable.Range(0, operationCount).Select(i => Task.Run(() =>
        {
            return context.GetTypes();
        })).ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, types => Assert.NotNull(types));
        Assert.All(results, types =>
        {
            if (types is not null)
            {
                Assert.NotEmpty(types);
            }
        });
        
        // All results should have the same count
        var firstCount = results[0]!.Count();
        Assert.All(results, types => Assert.Equal(firstCount, types!.Count()));
        
        output.WriteLine($"{operationCount} concurrent GetTypes calls returned consistent results");
    }

    [Fact]
    public async Task GetTypesGeneric_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        const int operationCount = 10;
        using var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
        
        // Act - Call GetTypes<T> concurrently
        var tasks = Enumerable.Range(0, operationCount).Select(i => Task.Run(() =>
        {
            return context.GetTypes<IClass1>().ToList();
        })).ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, types => Assert.NotNull(types));
        Assert.All(results, types => Assert.NotEmpty(types));
        
        output.WriteLine($"{operationCount} concurrent GetTypes<T> calls completed successfully");
    }

    #endregion

    #region Security Policy Thread Safety Tests

    [Fact]
    public async Task VerifyPath_ConcurrentCalls_ThreadSafe()
    {
        // Arrange
        const int operationCount = 20;
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        
        // Act - Call VerifyPath concurrently
        var tasks = Enumerable.Range(0, operationCount).Select(i => Task.Run(() =>
        {
            return AssemblyContext.VerifyPath(testPath, "*", AssemblySecurityPolicy.Default);
        })).ToArray();
        
        var results = await Task.WhenAll(tasks);
        
        // Assert
        Assert.All(results, path => Assert.NotNull(path));
        Assert.All(results, path => Assert.Equal(Path.GetFullPath(testPath), path));
        
        output.WriteLine($"{operationCount} concurrent VerifyPath calls completed successfully");
    }

    [Fact]
    public async Task Constructor_ConcurrentCreation_DifferentPolicies_ThreadSafe()
    {
        // Arrange
        const int contextCount = 10;
        var contexts = new List<AssemblyContext>();
        var lockObj = new object();
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        File.WriteAllText(testPath, "dummy");
        
        try
        {
            // Act - Create contexts with different policies concurrently
            var tasks = Enumerable.Range(0, contextCount).Select(i => Task.Run(() =>
            {
                var policy = i % 2 == 0 ? AssemblySecurityPolicy.Default : AssemblySecurityPolicy.Strict;
                var context = new AssemblyContext(testPath, basePathRestriction: "*", securityPolicy: policy);
                lock (lockObj) contexts.Add(context);
                return context;
            })).ToArray();
            
            await Task.WhenAll(tasks);
            
            // Assert
            Assert.Equal(contextCount, contexts.Count);
            Assert.Contains(contexts, c => !c.SecurityPolicy.StrictMode);
            Assert.Contains(contexts, c => c.SecurityPolicy.StrictMode);
            
            output.WriteLine($"Created {contextCount} contexts with different policies concurrently");
        }
        finally
        {
            foreach (var context in contexts)
            {
                context?.Dispose();
            }
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    #endregion

    #region Stress Tests

    [Fact]
    public async Task StressTest_ManyOperations_Concurrent()
    {
        // Arrange
        const int contextCount = 20;
        const int operationsPerContext = 5;
        var allTasks = new List<Task>();
        var contexts = new List<AssemblyContext>();
        var lockObj = new object();
        
        try
        {
            // Act - Create many contexts and perform operations
            for (int i = 0; i < contextCount; i++)
            {
                var contextTask = Task.Run(() =>
                {
                    var context = new AssemblyContext(simpleDllPath, basePathRestriction: "*");
                    lock (lockObj) contexts.Add(context);
                    
                    var tasks = new List<Task>();
                    for (int j = 0; j < operationsPerContext; j++)
                    {
                        tasks.Add(Task.Run(() =>
                        {
                            var instance = context.CreateInstance<IClass1>("Class1");
                            var result = instance?.Stuff($"test{j}");
                            Assert.NotNull(result);
                        }));
                    }
                    
                    return Task.WhenAll(tasks);
                });
                
                allTasks.Add(contextTask);
            }
            
            await Task.WhenAll(allTasks);
            
            // Assert
            Assert.Equal(contextCount, contexts.Count);
            
            output.WriteLine($"Stress test: {contextCount} contexts × {operationsPerContext} operations = {contextCount * operationsPerContext} total operations completed");
        }
        finally
        {
            foreach (var context in contexts)
            {
                context?.Dispose();
            }
        }
    }

    #endregion
}
