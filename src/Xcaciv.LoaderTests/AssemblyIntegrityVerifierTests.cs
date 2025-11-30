using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

using Xcaciv.Loader;

using Xunit;

namespace Xc.LoaderTests;

public class AssemblyIntegrityVerifierTests
{
    private readonly string tempDirectory;
    private readonly string testAssemblyPath;

    public AssemblyIntegrityVerifierTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"LoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        testAssemblyPath = Path.Combine(tempDirectory, "TestAssembly.dll");
        
        // Create a test file
        File.WriteAllText(testAssemblyPath, "Test assembly content for hashing");
    }

    [Fact]
    public void Constructor_Default_CreatesDisabledVerifier()
    {
        // Act
        var verifier = new AssemblyIntegrityVerifier();

        // Assert
        Assert.False(verifier.Enabled);
        Assert.False(verifier.LearningMode);
        Assert.Equal(HashAlgorithmName.SHA256, verifier.Algorithm);
        Assert.NotNull(verifier.HashStore);
        Assert.Equal(0, verifier.HashStore.Count);
    }

    [Fact]
    public void Constructor_EnabledWithLearning_CreatesLearningVerifier()
    {
        // Act
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        // Assert
        Assert.True(verifier.Enabled);
        Assert.True(verifier.LearningMode);
    }

    [Fact]
    public void Constructor_EnabledWithoutLearning_CreatesStrictVerifier()
    {
        // Act
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Assert
        Assert.True(verifier.Enabled);
        Assert.False(verifier.LearningMode);
    }

    [Fact]
    public void Constructor_CustomHashStore_UsesProvidedStore()
    {
        // Arrange
        var customStore = new AssemblyHashStore();
        customStore.AddOrUpdate(@"C:\Test\Assembly.dll", "preloadedHash==");

        // Act
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false,
            hashStore: customStore);

        // Assert
        Assert.Same(customStore, verifier.HashStore);
        Assert.Equal(1, verifier.HashStore.Count);
    }

    [Fact]
    public void VerifyIntegrity_Disabled_DoesNothing()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(); // Disabled by default

        // Act & Assert - should not throw
        verifier.VerifyIntegrity(testAssemblyPath);
    }

    [Fact]
    public void VerifyIntegrity_LearningMode_AddsNewHash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        // Act
        verifier.VerifyIntegrity(testAssemblyPath);

        // Assert
        Assert.Equal(1, verifier.HashStore.Count);
        Assert.True(verifier.HashStore.TryGetHash(testAssemblyPath, out var hash));
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
    }

    [Fact]
    public void VerifyIntegrity_LearningMode_RaisesHashLearnedEvent()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        string? capturedPath = null;
        string? capturedHash = null;

        verifier.HashLearned += (path, hash) =>
        {
            capturedPath = path;
            capturedHash = hash;
        };

        // Act
        verifier.VerifyIntegrity(testAssemblyPath);

        // Assert
        Assert.NotNull(capturedPath);
        Assert.NotNull(capturedHash);
        Assert.Contains("TestAssembly.dll", capturedPath);
    }

    [Fact]
    public void VerifyIntegrity_StrictMode_NoHash_ThrowsSecurityException()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false); // Strict mode

        // Act & Assert
        var ex = Assert.Throws<SecurityException>(() => 
            verifier.VerifyIntegrity(testAssemblyPath));
        
        Assert.Contains("No trusted hash found", ex.Message);
        Assert.Contains(testAssemblyPath, ex.Message);
    }

    [Fact]
    public void VerifyIntegrity_MatchingHash_Succeeds()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Pre-compute and store the correct hash
        var correctHash = verifier.ComputeHash(testAssemblyPath);
        verifier.HashStore.AddOrUpdate(testAssemblyPath, correctHash);

        // Act & Assert - should not throw
        verifier.VerifyIntegrity(testAssemblyPath);
    }

    [Fact]
    public void VerifyIntegrity_MismatchedHash_ThrowsSecurityException()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Store incorrect hash
        verifier.HashStore.AddOrUpdate(testAssemblyPath, "incorrectHash==");

        // Act & Assert
        var ex = Assert.Throws<SecurityException>(() =>
            verifier.VerifyIntegrity(testAssemblyPath));

        Assert.Contains("integrity verification failed", ex.Message);
        Assert.Contains("incorrectHash==", ex.Message);
    }

    [Fact]
    public void VerifyIntegrity_MismatchedHash_RaisesHashMismatchEvent()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        verifier.HashStore.AddOrUpdate(testAssemblyPath, "incorrectHash==");

        string? capturedPath = null;
        string? capturedExpected = null;
        string? capturedActual = null;

        verifier.HashMismatchDetected += (path, expected, actual) =>
        {
            capturedPath = path;
            capturedExpected = expected;
            capturedActual = actual;
        };

        // Act
        try
        {
            verifier.VerifyIntegrity(testAssemblyPath);
        }
        catch (SecurityException)
        {
            // Expected
        }

        // Assert
        Assert.NotNull(capturedPath);
        Assert.Equal("incorrectHash==", capturedExpected);
        Assert.NotNull(capturedActual);
        Assert.NotEqual(capturedExpected, capturedActual);
    }

    [Fact]
    public void VerifyIntegrity_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        var nonExistentPath = Path.Combine(tempDirectory, "NonExistent.dll");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            verifier.VerifyIntegrity(nonExistentPath));
    }

    [Fact]
    public void VerifyIntegrity_NullPath_ThrowsArgumentException()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => verifier.VerifyIntegrity(null!));
        Assert.Throws<ArgumentException>(() => verifier.VerifyIntegrity(""));
        Assert.Throws<ArgumentException>(() => verifier.VerifyIntegrity("   "));
    }

    [Fact]
    public void ComputeHash_SHA256_ReturnsValidBase64Hash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA256);

        // Act
        var hash = verifier.ComputeHash(testAssemblyPath);

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        
        // Verify it's valid base64
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(32, bytes.Length); // SHA256 is 256 bits = 32 bytes
    }

    [Fact]
    public void ComputeHash_SHA384_ReturnsValidBase64Hash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA384);

        // Act
        var hash = verifier.ComputeHash(testAssemblyPath);

        // Assert
        Assert.NotNull(hash);
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(48, bytes.Length); // SHA384 is 384 bits = 48 bytes
    }

    [Fact]
    public void ComputeHash_SHA512_ReturnsValidBase64Hash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            algorithm: HashAlgorithmName.SHA512);

        // Act
        var hash = verifier.ComputeHash(testAssemblyPath);

        // Assert
        Assert.NotNull(hash);
        var bytes = Convert.FromBase64String(hash);
        Assert.Equal(64, bytes.Length); // SHA512 is 512 bits = 64 bytes
    }

    [Fact]
    public void ComputeHash_SameFile_ProducesSameHash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);

        // Act
        var hash1 = verifier.ComputeHash(testAssemblyPath);
        var hash2 = verifier.ComputeHash(testAssemblyPath);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentContent_ProducesDifferentHash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);
        
        var file1 = Path.Combine(tempDirectory, "File1.dll");
        var file2 = Path.Combine(tempDirectory, "File2.dll");
        
        File.WriteAllText(file1, "Content A");
        File.WriteAllText(file2, "Content B");

        // Act
        var hash1 = verifier.ComputeHash(file1);
        var hash2 = verifier.ComputeHash(file2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void TrustAssembly_AddsHashToStore()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        // Act
        verifier.TrustAssembly(testAssemblyPath);

        // Assert
        Assert.Equal(1, verifier.HashStore.Count);
        Assert.True(verifier.HashStore.TryGetHash(testAssemblyPath, out var hash));
        Assert.NotNull(hash);
    }

    [Fact]
    public void UntrustAssembly_RemovesHashFromStore()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: false);

        verifier.TrustAssembly(testAssemblyPath);
        Assert.Equal(1, verifier.HashStore.Count);

        // Act
        var result = verifier.UntrustAssembly(testAssemblyPath);

        // Assert
        Assert.True(result);
        Assert.Equal(0, verifier.HashStore.Count);
    }

    [Fact]
    public void UntrustAssembly_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);

        // Act
        var result = verifier.UntrustAssembly(@"C:\NonExistent\Assembly.dll");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsTrusted_TrustedAssembly_ReturnsTrue()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);
        verifier.TrustAssembly(testAssemblyPath);

        // Act
        var result = verifier.IsTrusted(testAssemblyPath);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsTrusted_UntrustedAssembly_ReturnsFalse()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);

        // Act
        var result = verifier.IsTrusted(testAssemblyPath);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void LearningMode_ExistingHash_DoesNotOverwrite()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(
            enabled: true,
            learningMode: true);

        var originalHash = verifier.ComputeHash(testAssemblyPath);
        verifier.HashStore.AddOrUpdate(testAssemblyPath, "manualHash==");

        // Act & Assert - Existing hash should be verified even in learning mode
        var ex = Assert.Throws<SecurityException>(() => 
            verifier.VerifyIntegrity(testAssemblyPath));
        
        Assert.Contains("integrity verification failed", ex.Message);
        Assert.Contains("manualHash==", ex.Message);
        
        // Hash should not be replaced
        Assert.True(verifier.HashStore.TryGetHash(testAssemblyPath, out var storedHash));
        Assert.Equal("manualHash==", storedHash); // Should not be replaced
    }

    [Fact]
    public void FileModification_ChangesHash()
    {
        // Arrange
        var verifier = new AssemblyIntegrityVerifier(enabled: true);
        var modifiableFile = Path.Combine(tempDirectory, "Modifiable.dll");
        
        File.WriteAllText(modifiableFile, "Original content");
        var originalHash = verifier.ComputeHash(modifiableFile);

        // Act - Modify the file
        File.AppendAllText(modifiableFile, " Modified");
        var modifiedHash = verifier.ComputeHash(modifiableFile);

        // Assert
        Assert.NotEqual(originalHash, modifiedHash);
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
