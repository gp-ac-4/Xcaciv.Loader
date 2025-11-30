using System;
using System.IO;

using Xcaciv.Loader;

using Xunit;

namespace Xc.LoaderTests;

public class AssemblyPathValidatorTests
{
    [Fact]
    public void SanitizeAssemblyPath_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AssemblyPathValidator.SanitizeAssemblyPath(null!));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.SanitizeAssemblyPath(""));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.SanitizeAssemblyPath("   "));
    }

    [Fact]
    public void SanitizeAssemblyPath_RemovesNullBytes()
    {
        // Arrange
        var pathWithNullByte = "C:\\Test\0Assembly.dll";

        // Act
        var result = AssemblyPathValidator.SanitizeAssemblyPath(pathWithNullByte);

        // Assert
        Assert.DoesNotContain('\0', result);
        Assert.Equal("C:\\TestAssembly.dll", result);
    }

    [Fact]
    public void SanitizeAssemblyPath_NormalizesForwardSlashes()
    {
        // Arrange
        var pathWithForwardSlashes = "C:/Test/Assembly.dll";

        // Act
        var result = AssemblyPathValidator.SanitizeAssemblyPath(pathWithForwardSlashes);

        // Assert
        Assert.DoesNotContain('/', result);
        Assert.Contains(Path.DirectorySeparatorChar.ToString(), result);
    }

    [Fact]
    public void SanitizeAssemblyPath_RemovesDoubleSeparators()
    {
        // Arrange
        var pathWithDouble = $"C:{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}Test{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}Assembly.dll";

        // Act
        var result = AssemblyPathValidator.SanitizeAssemblyPath(pathWithDouble);

        // Assert
        var doubleSep = $"{Path.DirectorySeparatorChar}{Path.DirectorySeparatorChar}";
        Assert.DoesNotContain(doubleSep, result);
    }

    [Fact]
    public void SanitizeAssemblyPath_TrimsWhitespace()
    {
        // Arrange
        var pathWithWhitespace = "  C:\\Test\\Assembly.dll  ";

        // Act
        var result = AssemblyPathValidator.SanitizeAssemblyPath(pathWithWhitespace);

        // Assert
        Assert.False(result.StartsWith(" "), "Result should not start with whitespace");
        Assert.False(result.EndsWith(" "), "Result should not end with whitespace");
    }

    [Fact]
    public void ResolveRelativeToBase_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AssemblyPathValidator.ResolveRelativeToBase(null!));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.ResolveRelativeToBase(""));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.ResolveRelativeToBase("   "));
    }

    [Fact]
    public void ResolveRelativeToBase_RelativePath_ReturnsAbsolutePath()
    {
        // Arrange
        var relativePath = "Plugins\\MyPlugin.dll";

        // Act
        var result = AssemblyPathValidator.ResolveRelativeToBase(relativePath);

        // Assert
        Assert.True(Path.IsPathRooted(result));
        Assert.Contains(AppDomain.CurrentDomain.BaseDirectory, result);
        Assert.EndsWith("MyPlugin.dll", result);
    }

    [Fact]
    public void ResolveRelativeToBase_PathWithDotDot_Normalizes()
    {
        // Arrange
        var relativePath = "Plugins\\..\\OtherPlugins\\MyPlugin.dll";

        // Act
        var result = AssemblyPathValidator.ResolveRelativeToBase(relativePath);

        // Assert
        Assert.True(Path.IsPathRooted(result));
        Assert.DoesNotContain("..", result);
        Assert.Contains("OtherPlugins", result);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsSafePath_NullOrEmpty_ReturnsFalse(string path, bool expected)
    {
        // Act
        var result = AssemblyPathValidator.IsSafePath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSafePath_PathWithNullByte_ReturnsFalse()
    {
        // Arrange
        var path = "C:\\Test\0Assembly.dll";

        // Act
        var result = AssemblyPathValidator.IsSafePath(path);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsSafePath_PathWithDotDot_ReturnsFalse()
    {
        // Arrange
        var path = "C:\\Test\\..\\..\\Windows\\System32\\test.dll";

        // Act
        var result = AssemblyPathValidator.IsSafePath(path);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData("C:\\Test\\*.dll", false)]
    [InlineData("C:\\Test\\?.dll", false)]
    [InlineData("C:\\Test\\<test>.dll", false)]
    [InlineData("C:\\Test\\>test.dll", false)]
    [InlineData("C:\\Test\\|test.dll", false)]
    public void IsSafePath_PathWithDangerousCharacters_ReturnsFalse(string path, bool expected)
    {
        // Act
        var result = AssemblyPathValidator.IsSafePath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsSafePath_ValidPath_ReturnsTrue()
    {
        // Arrange
        var path = "C:\\Test\\Assembly.dll";

        // Act
        var result = AssemblyPathValidator.IsSafePath(path);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasValidAssemblyExtension_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AssemblyPathValidator.HasValidAssemblyExtension(null!));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.HasValidAssemblyExtension(""));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.HasValidAssemblyExtension("   "));
    }

    [Theory]
    [InlineData("Assembly.dll", true)]
    [InlineData("Assembly.DLL", true)]
    [InlineData("Assembly.exe", true)]
    [InlineData("Assembly.EXE", true)]
    [InlineData("C:\\Test\\Assembly.dll", true)]
    public void HasValidAssemblyExtension_ValidExtension_ReturnsTrue(string path, bool expected)
    {
        // Act
        var result = AssemblyPathValidator.HasValidAssemblyExtension(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Assembly.txt", false)]
    [InlineData("Assembly.so", false)]
    [InlineData("Assembly.dylib", false)]
    [InlineData("Assembly", false)]
    [InlineData("Assembly.dll.txt", false)]
    public void HasValidAssemblyExtension_InvalidExtension_ReturnsFalse(string path, bool expected)
    {
        // Act
        var result = AssemblyPathValidator.HasValidAssemblyExtension(path);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidateAndSanitize_NullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => AssemblyPathValidator.ValidateAndSanitize(null!));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.ValidateAndSanitize(""));
        Assert.Throws<ArgumentException>(() => AssemblyPathValidator.ValidateAndSanitize("   "));
    }

    [Fact]
    public void ValidateAndSanitize_UnsafePath_ThrowsArgumentException()
    {
        // Arrange
        var unsafePath = "C:\\Test\\..\\..\\Windows\\test.dll";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            AssemblyPathValidator.ValidateAndSanitize(unsafePath));
        Assert.Contains("failed safety checks", ex.Message);
    }

    [Fact]
    public void ValidateAndSanitize_InvalidExtension_ThrowsArgumentException()
    {
        // Arrange
        var invalidPath = "C:\\Test\\Assembly.txt";

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            AssemblyPathValidator.ValidateAndSanitize(invalidPath));
        Assert.Contains("valid assembly extension", ex.Message);
    }

    [Fact]
    public void ValidateAndSanitize_ValidPath_ReturnsSanitizedPath()
    {
        // Arrange
        var validPath = "C:/Test//Assembly.dll";

        // Act
        var result = AssemblyPathValidator.ValidateAndSanitize(validPath);

        // Assert
        Assert.DoesNotContain("//", result);
        Assert.DoesNotContain("/", result);
        Assert.Contains("Assembly.dll", result);
    }

    [Fact]
    public void ValidateAndSanitize_RelativePathWithResolve_ReturnsAbsolutePath()
    {
        // Arrange
        var relativePath = "Plugins/MyPlugin.dll";

        // Act
        var result = AssemblyPathValidator.ValidateAndSanitize(
            relativePath,
            resolveRelativeToBase: true);

        // Assert
        Assert.True(Path.IsPathRooted(result));
        Assert.Contains(AppDomain.CurrentDomain.BaseDirectory, result);
        Assert.EndsWith("MyPlugin.dll", result);
    }

    [Fact]
    public void ValidateAndSanitize_RelativePathWithoutResolve_ReturnsSanitizedRelativePath()
    {
        // Arrange
        var relativePath = "Plugins/MyPlugin.dll";

        // Act
        var result = AssemblyPathValidator.ValidateAndSanitize(
            relativePath,
            resolveRelativeToBase: false);

        // Assert
        Assert.DoesNotContain("/", result);
        Assert.Contains("MyPlugin.dll", result);
    }

    [Fact]
    public void ValidateAndSanitize_ComplexScenario_Success()
    {
        // Arrange - path with multiple issues that should be fixed
        var messyPath = "  C:/Test//Plugins\\Assembly.dll  ";

        // Act
        var result = AssemblyPathValidator.ValidateAndSanitize(messyPath);

        // Assert
        Assert.False(result.StartsWith(" "), "Result should not start with whitespace");
        Assert.False(result.EndsWith(" "), "Result should not end with whitespace");
        Assert.DoesNotContain("//", result);
        Assert.DoesNotContain("/", result);
        Assert.Contains("Assembly.dll", result);
    }

    [Theory]
    [InlineData("C:\\Test\\*.dll")]
    [InlineData("C:\\Test\\Assembly\0.dll")]
    [InlineData("C:\\Test\\..\\..\\Windows\\test.dll")]
    public void ValidateAndSanitize_VariousDangerousPatterns_ThrowsArgumentException(string dangerousPath)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            AssemblyPathValidator.ValidateAndSanitize(dangerousPath));
    }
}
