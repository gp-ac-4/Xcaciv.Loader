using System;
using System.IO;

using Xcaciv.Loader;

using Xunit;

namespace Xc.LoaderTests;

public class AssemblyHashStoreTests : IDisposable
{
    private readonly string tempDirectory;
    private readonly string testCsvPath;

    public AssemblyHashStoreTests()
    {
        tempDirectory = Path.Combine(Path.GetTempPath(), $"LoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        testCsvPath = Path.Combine(tempDirectory, "test-hashes.csv");
    }

    [Fact]
    public void Constructor_CreatesEmptyStore()
    {
        // Act
        var store = new AssemblyHashStore();

        // Assert
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void AddOrUpdate_AddsNewHash()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var path = @"C:\Test\Assembly.dll";
        var hash = "abc123hash==";

        // Act
        store.AddOrUpdate(path, hash);

        // Assert
        Assert.Equal(1, store.Count);
        Assert.True(store.TryGetHash(path, out var retrievedHash));
        Assert.Equal(hash, retrievedHash);
    }

    [Fact]
    public void AddOrUpdate_UpdatesExistingHash()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var path = @"C:\Test\Assembly.dll";
        var oldHash = "oldHash==";
        var newHash = "newHash==";

        // Act
        store.AddOrUpdate(path, oldHash);
        store.AddOrUpdate(path, newHash);

        // Assert
        Assert.Equal(1, store.Count);
        Assert.True(store.TryGetHash(path, out var retrievedHash));
        Assert.Equal(newHash, retrievedHash);
    }

    [Fact]
    public void AddOrUpdate_NullPath_ThrowsArgumentException()
    {
        // Arrange
        var store = new AssemblyHashStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => store.AddOrUpdate(null!, "hash"));
        Assert.Throws<ArgumentException>(() => store.AddOrUpdate("", "hash"));
        Assert.Throws<ArgumentException>(() => store.AddOrUpdate("   ", "hash"));
    }

    [Fact]
    public void AddOrUpdate_NullHash_ThrowsArgumentException()
    {
        // Arrange
        var store = new AssemblyHashStore();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => store.AddOrUpdate(@"C:\Test\Assembly.dll", null!));
        Assert.Throws<ArgumentException>(() => store.AddOrUpdate(@"C:\Test\Assembly.dll", ""));
        Assert.Throws<ArgumentException>(() => store.AddOrUpdate(@"C:\Test\Assembly.dll", "   "));
    }

    [Fact]
    public void TryGetHash_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var store = new AssemblyHashStore();

        // Act
        var result = store.TryGetHash(@"C:\NonExistent\Assembly.dll", out var hash);

        // Assert
        Assert.False(result);
        Assert.Null(hash);
    }

    [Fact]
    public void TryGetHash_NormalizesPath()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var relativePath = @".\Test\Assembly.dll";
        var hash = "testHash==";

        // Act
        store.AddOrUpdate(relativePath, hash);
        var absolutePath = Path.GetFullPath(relativePath);
        var result = store.TryGetHash(absolutePath, out var retrievedHash);

        // Assert - paths should be normalized (relative vs absolute) and match
        Assert.True(result);
        Assert.Equal(hash, retrievedHash);
    }

    [Fact]
    public void Remove_ExistingPath_ReturnsTrue()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var path = @"C:\Test\Assembly.dll";
        store.AddOrUpdate(path, "hash==");

        // Act
        var result = store.Remove(path);

        // Assert
        Assert.True(result);
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void Remove_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var store = new AssemblyHashStore();

        // Act
        var result = store.Remove(@"C:\NonExistent\Assembly.dll");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Clear_RemovesAllHashes()
    {
        // Arrange
        var store = new AssemblyHashStore();
        store.AddOrUpdate(@"C:\Test\Assembly1.dll", "hash1==");
        store.AddOrUpdate(@"C:\Test\Assembly2.dll", "hash2==");
        store.AddOrUpdate(@"C:\Test\Assembly3.dll", "hash3==");

        // Act
        store.Clear();

        // Assert
        Assert.Equal(0, store.Count);
    }

    [Fact]
    public void SaveToFile_CreatesValidCsvFile()
    {
        // Arrange
        var store = new AssemblyHashStore();
        store.AddOrUpdate(@"C:\Test\Assembly1.dll", "hash1==");
        store.AddOrUpdate(@"C:\Test\Assembly2.dll", "hash2==");

        // Act
        store.SaveToFile(testCsvPath);

        // Assert
        Assert.True(File.Exists(testCsvPath));
        var content = File.ReadAllText(testCsvPath);
        Assert.Contains("# Assembly Integrity Hash Store", content);
        Assert.Contains("Assembly1.dll", content);
        Assert.Contains("Assembly2.dll", content);
        Assert.Contains("hash1==", content);
        Assert.Contains("hash2==", content);
    }

    [Fact]
    public void LoadFromFile_LoadsHashesCorrectly()
    {
        // Arrange
        var store1 = new AssemblyHashStore();
        store1.AddOrUpdate(@"C:\Test\Assembly1.dll", "hash1==");
        store1.AddOrUpdate(@"C:\Test\Assembly2.dll", "hash2==");
        store1.SaveToFile(testCsvPath);

        var store2 = new AssemblyHashStore();

        // Act
        store2.LoadFromFile(testCsvPath);

        // Assert
        Assert.Equal(2, store2.Count);
        Assert.True(store2.TryGetHash(@"C:\Test\Assembly1.dll", out var hash1));
        Assert.Equal("hash1==", hash1);
        Assert.True(store2.TryGetHash(@"C:\Test\Assembly2.dll", out var hash2));
        Assert.Equal("hash2==", hash2);
    }

    [Fact]
    public void LoadFromFile_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var nonExistentPath = Path.Combine(tempDirectory, "nonexistent.csv");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => store.LoadFromFile(nonExistentPath));
    }

    [Fact]
    public void LoadFromFile_InvalidFormat_ThrowsFormatException()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var invalidCsvPath = Path.Combine(tempDirectory, "invalid.csv");
        File.WriteAllText(invalidCsvPath, "InvalidLine\nAnother,Invalid,Line,TooMany");

        // Act & Assert
        Assert.Throws<FormatException>(() => store.LoadFromFile(invalidCsvPath));
    }

    [Fact]
    public void LoadFromFile_SkipsComments()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var csvPath = Path.Combine(tempDirectory, "withcomments.csv");
        File.WriteAllText(csvPath, 
            "# This is a comment\n" +
            "C:\\Test\\Assembly.dll,hash==\n" +
            "# Another comment\n");

        // Act
        store.LoadFromFile(csvPath);

        // Assert
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void LoadFromFile_SkipsEmptyLines()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var csvPath = Path.Combine(tempDirectory, "withempty.csv");
        File.WriteAllText(csvPath,
            "\n" +
            "C:\\Test\\Assembly.dll,hash==\n" +
            "\n" +
            "   \n");

        // Act
        store.LoadFromFile(csvPath);

        // Assert
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void LoadFromFile_ReplacesExistingHashes()
    {
        // Arrange
        var store = new AssemblyHashStore();
        store.AddOrUpdate(@"C:\Test\Assembly1.dll", "oldHash==");
        
        var csvPath = Path.Combine(tempDirectory, "replacement.csv");
        File.WriteAllText(csvPath, "C:\\Test\\Assembly2.dll,newHash==");

        // Act
        store.LoadFromFile(csvPath);

        // Assert
        Assert.Equal(1, store.Count);
        Assert.False(store.TryGetHash(@"C:\Test\Assembly1.dll", out _));
        Assert.True(store.TryGetHash(@"C:\Test\Assembly2.dll", out _));
    }

    [Fact]
    public void MergeFromFile_MergesWithExisting_NoOverwrite()
    {
        // Arrange
        var store = new AssemblyHashStore();
        store.AddOrUpdate(@"C:\Test\Assembly1.dll", "hash1==");
        store.AddOrUpdate(@"C:\Test\Assembly2.dll", "hash2old==");

        var csvPath = Path.Combine(tempDirectory, "merge.csv");
        File.WriteAllText(csvPath,
            "C:\\Test\\Assembly2.dll,hash2new==\n" +
            "C:\\Test\\Assembly3.dll,hash3==");

        // Act
        store.MergeFromFile(csvPath, overwriteExisting: false);

        // Assert
        Assert.Equal(3, store.Count);
        Assert.True(store.TryGetHash(@"C:\Test\Assembly2.dll", out var hash2));
        Assert.Equal("hash2old==", hash2); // Should NOT be overwritten
        Assert.True(store.TryGetHash(@"C:\Test\Assembly3.dll", out var hash3));
        Assert.Equal("hash3==", hash3);
    }

    [Fact]
    public void MergeFromFile_MergesWithExisting_WithOverwrite()
    {
        // Arrange
        var store = new AssemblyHashStore();
        store.AddOrUpdate(@"C:\Test\Assembly1.dll", "hash1==");
        store.AddOrUpdate(@"C:\Test\Assembly2.dll", "hash2old==");

        var csvPath = Path.Combine(tempDirectory, "merge.csv");
        File.WriteAllText(csvPath,
            "C:\\Test\\Assembly2.dll,hash2new==\n" +
            "C:\\Test\\Assembly3.dll,hash3==");

        // Act
        store.MergeFromFile(csvPath, overwriteExisting: true);

        // Assert
        Assert.Equal(3, store.Count);
        Assert.True(store.TryGetHash(@"C:\Test\Assembly2.dll", out var hash2));
        Assert.Equal("hash2new==", hash2); // SHOULD be overwritten
        Assert.True(store.TryGetHash(@"C:\Test\Assembly3.dll", out var hash3));
        Assert.Equal("hash3==", hash3);
    }

    [Fact]
    public void GetFilePaths_ReturnsAllPaths()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var path1 = @"C:\Test\Assembly1.dll";
        var path2 = @"C:\Test\Assembly2.dll";
        var path3 = @"C:\Test\Assembly3.dll";
        
        store.AddOrUpdate(path1, "hash1==");
        store.AddOrUpdate(path2, "hash2==");
        store.AddOrUpdate(path3, "hash3==");

        // Act
        var paths = store.GetFilePaths();

        // Assert
        Assert.Equal(3, paths.Count);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesData()
    {
        // Arrange
        var store1 = new AssemblyHashStore();
        var testData = new[]
        {
            (@"C:\Test\Assembly1.dll", "hash1base64=="),
            (@"C:\Test\Path With Spaces\Assembly2.dll", "hash2base64=="),
            (@"C:\Test\Path,With,Commas\Assembly3.dll", "hash3base64=="),
        };

        foreach (var (path, hash) in testData)
        {
            store1.AddOrUpdate(path, hash);
        }

        // Act - Save and load
        store1.SaveToFile(testCsvPath);
        var store2 = new AssemblyHashStore();
        store2.LoadFromFile(testCsvPath);

        // Assert
        Assert.Equal(testData.Length, store2.Count);
        foreach (var (path, expectedHash) in testData)
        {
            Assert.True(store2.TryGetHash(path, out var actualHash));
            Assert.Equal(expectedHash, actualHash);
        }
    }

    [Fact]
    public void CsvFormat_HandlesPathsWithCommas()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var pathWithComma = @"C:\Test\Path,With,Commas\Assembly.dll";
        var hash = "testHash==";

        // Act
        store.AddOrUpdate(pathWithComma, hash);
        store.SaveToFile(testCsvPath);

        var loadedStore = new AssemblyHashStore();
        loadedStore.LoadFromFile(testCsvPath);

        // Assert
        Assert.True(loadedStore.TryGetHash(pathWithComma, out var retrievedHash));
        Assert.Equal(hash, retrievedHash);
    }

    [Fact]
    public void CsvFormat_HandlesPathsWithQuotes()
    {
        // Arrange
        var store = new AssemblyHashStore();
        var pathWithQuote = @"C:\Test\Path""With""Quotes\Assembly.dll";
        var hash = "testHash==";

        // Act
        store.AddOrUpdate(pathWithQuote, hash);
        store.SaveToFile(testCsvPath);

        var loadedStore = new AssemblyHashStore();
        loadedStore.LoadFromFile(testCsvPath);

        // Assert
        Assert.True(loadedStore.TryGetHash(pathWithQuote, out var retrievedHash));
        Assert.Equal(hash, retrievedHash);
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
