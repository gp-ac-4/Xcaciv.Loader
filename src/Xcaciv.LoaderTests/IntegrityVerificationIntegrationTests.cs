using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

using Xcaciv.Loader;

using Xunit;

namespace Xc.LoaderTests;

public class IntegrityVerificationIntegrationTests
{
    private readonly string tempDirectory;
    private readonly string testDllPath;

    private string simpleDllPath = @"..\..\..\..\TestAssembly\bin\{1}\net8.0\zTestAssembly.dll";

    public IntegrityVerificationIntegrationTests()
    {

#if DEBUG
        this.simpleDllPath = simpleDllPath.Replace("{1}", "Debug");
#else
        this.simpleDllPath = simpleDllPath.Replace("{1}", "Release");
#endif

        // resolve absolute paths
        var sourceAssembly = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, this.simpleDllPath));

        tempDirectory = Path.Combine(Path.GetTempPath(), $"LoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        
        // Copy test assembly
        testDllPath = Path.Combine(tempDirectory, "zTestAssembly.dll");

        if (File.Exists(sourceAssembly))
        {
            File.Copy(sourceAssembly, testDllPath, true);
        }
        else
        {
            throw new Exception("Test assembly not found: " + sourceAssembly);
        }
    }

    [Fact]
    public void AssemblyContext_NoVerifier_LoadsNormally()
    {
        // Arrange & Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*");

        // Assert - should load without verification
        var instance = context.CreateInstance("Class1");
        Assert.NotNull(instance);
    }

    [Fact]
    public void AssemblyContext_DisabledVerifier_LoadsNormally()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(); // Disabled by default

        // Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier);

        // Assert
        var instance = context.CreateInstance("Class1");
        Assert.NotNull(instance);
    }

    [Fact]
    public void AssemblyContext_LearningMode_FirstLoad_Learns()
    {
        if (!File.Exists(testDllPath))
        {
            // Skip if test assembly not available
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        bool hashLearned = false;
        verifier.HashLearned += (path, hash) => hashLearned = true;

        // Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier);

        var instance = context.CreateInstance("Class1");

        // Assert
        Assert.NotNull(instance);
        Assert.True(hashLearned);
        Assert.Equal(1, verifier.HashStore.Count);
    }

    [Fact]
    public void AssemblyContext_LearningMode_SecondLoad_Verifies()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        // First load - learns
        using (var context1 = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier))
        {
            context1.CreateInstance("Class1");
        }

        // Act - Second load with same verifier - should verify
        using var context2 = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier);

        var instance = context2.CreateInstance("Class1");

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(1, verifier.HashStore.Count); // Still just one hash
    }

    [Fact]
    public void AssemblyContext_StrictMode_NoHash_ThrowsSecurityException()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false); // Strict mode

        // Act & Assert
        Assert.Throws<SecurityException>(() =>
        {
            using var context = new AssemblyContext(
                testDllPath,
                basePathRestriction: "*",
                integrityVerifier: verifier);
            
            // Must actually load the assembly to trigger verification
            context.CreateInstance("Class1");
        });
    }

    [Fact]
    public void AssemblyContext_StrictMode_WithValidHash_Succeeds()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Pre-trust the assembly
        verifier.TrustAssembly(testDllPath);

        // Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier);

        var instance = context.CreateInstance("Class1");

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void AssemblyContext_StrictMode_TamperedFile_ThrowsSecurityException()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Trust with original hash
        verifier.TrustAssembly(testDllPath);

        // Tamper with the file
        File.AppendAllText(testDllPath, "TAMPERED");

        // Act & Assert
        Assert.Throws<SecurityException>(() =>
        {
            using var context = new AssemblyContext(
                testDllPath,
                basePathRestriction: "*",
                integrityVerifier: verifier);

           context.CreateInstance("Class1");
        });
    }

    [Fact]
    public void WorkflowTest_DevelopmentToProduction()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        var hashFilePath = Path.Combine(tempDirectory, "trusted-hashes.csv");

        // DEVELOPMENT: Learn hashes
        var devVerifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        using (var devContext = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: devVerifier))
        {
            devContext.CreateInstance("Class1");
        }

        // Save learned hashes
        devVerifier.HashStore.SaveToFile(hashFilePath);

        // PRODUCTION: Load hashes and verify in strict mode
        var prodStore = new AssemblyHashStore();
        prodStore.LoadFromFile(hashFilePath);

        var prodVerifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false,
            hashStore: prodStore);

        // Act
        using var prodContext = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: prodVerifier);

        var instance = prodContext.CreateInstance("Class1");

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void MultipleAssemblies_IndependentVerification()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Create second test file
        var testDll2Path = Path.Combine(tempDirectory, "TestAssembly2.dll");
        File.Copy(testDllPath, testDll2Path, true);
        
        // Modify it slightly
        File.AppendAllText(testDll2Path, " ");

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        // Act - Load first assembly
        using (var context1 = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier))
        {
            context1.CreateInstance("Class1");
        }

        // Load second assembly
        using (var context2 = new AssemblyContext(
            testDll2Path,
            basePathRestriction: "*",
            integrityVerifier: verifier))
        {
            context2.CreateInstance("Class1");
        }

        // Assert - Both should be learned
        Assert.Equal(2, verifier.HashStore.Count);
        Assert.True(verifier.IsTrusted(testDllPath));
        Assert.True(verifier.IsTrusted(testDll2Path));
    }

    [Fact]
    public void EventMonitoring_CapturesAllEvents()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        string? learnedPath = null;
        string? learnedHash = null;

        verifier.HashLearned += (path, hash) =>
        {
            learnedPath = path;
            learnedHash = hash;
        };

        // Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: "*",
            integrityVerifier: verifier);

        context.CreateInstance("Class1");

        // Assert
        Assert.NotNull(learnedPath);
        Assert.NotNull(learnedHash);
        Assert.Contains(testDllPath, learnedPath);
    }

    [Fact]
    public void DifferentHashAlgorithms_ProduceDifferentHashes()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange
        var verifier256 = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA256);

        var verifier384 = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA384);

        var verifier512 = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA512);

        // Act
        var hash256 = verifier256.ComputeHash(testDllPath);
        var hash384 = verifier384.ComputeHash(testDllPath);
        var hash512 = verifier512.ComputeHash(testDllPath);

        // Assert
        Assert.NotEqual(hash256, hash384);
        Assert.NotEqual(hash384, hash512);
        Assert.NotEqual(hash256, hash512);
        
        // Verify lengths
        Assert.Equal(32, Convert.FromBase64String(hash256).Length);
        Assert.Equal(48, Convert.FromBase64String(hash384).Length);
        Assert.Equal(64, Convert.FromBase64String(hash512).Length);
    }

    [Fact]
    public void CombinedSecurity_PathRestrictionAndIntegrity()
    {
        if (!File.Exists(testDllPath))
        {
            return;
        }

        // Arrange - Combine base path restriction with integrity verification
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        verifier.TrustAssembly(testDllPath);

        var securityPolicy = AssemblySecurityPolicy.Strict;

        // Act
        using var context = new AssemblyContext(
            testDllPath,
            basePathRestriction: tempDirectory,
            securityPolicy: securityPolicy,
            integrityVerifier: verifier);

        var instance = context.CreateInstance("Class1");

        // Assert - both security layers passed
        Assert.NotNull(instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(tempDirectory))
        {
            try
            {
                Directory.Delete(tempDirectory, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
}
