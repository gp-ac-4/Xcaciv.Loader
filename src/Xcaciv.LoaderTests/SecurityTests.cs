using System;
using System.Security;

using Xcaciv.Loader;

using Xunit;

namespace Xc.LoaderTests;

public class SecurityTests
{
    [Fact]
    public void StrictDirectoryRestriction_BlocksSystemDirectories()
    {
        // Use strict security policy
        var strictPolicy = AssemblySecurityPolicy.Strict;
        
        // This should throw a SecurityException because it points to a system directory
        string testPath = @"C:\Windows\System32\kernel32.dll";
        
        var exception = Assert.Throws<SecurityException>(() => 
            AssemblyContext.VerifyPath(testPath, "*", strictPolicy));
        
        Assert.Contains("system directories", exception.Message, StringComparison.OrdinalIgnoreCase);
        
        // Verify strict mode is enabled
        Assert.True(strictPolicy.StrictMode);
    }
    
    [Fact]
    public void DefaultMode_ChecksBasicRestrictions()
    {
        // Use default security policy
        var defaultPolicy = AssemblySecurityPolicy.Default;
        
        // Verify default mode can still validate paths
        string validPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(SecurityTests).Assembly.Location) ?? String.Empty, 
            "test.dll");
            
        string result = AssemblyContext.VerifyPath(validPath, "*", defaultPolicy);
        
        Assert.NotNull(result);
        Assert.False(defaultPolicy.StrictMode);
    }
    
    [Fact]
    public void AssemblyContext_UsesDefaultPolicyWhenNotSpecified()
    {
        // Arrange
        string testPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(SecurityTests).Assembly.Location) ?? String.Empty, 
            "test.dll");
        
        // Create file for test
        System.IO.File.WriteAllText(testPath, "test");
        
        try
        {
            // Act
            using var context = new AssemblyContext(testPath, basePathRestriction: "*");
            
            // Assert
            Assert.NotNull(context.SecurityPolicy);
            Assert.False(context.SecurityPolicy.StrictMode);
            Assert.Equal(AssemblySecurityPolicy.Default.ForbiddenDirectories.Count, 
                context.SecurityPolicy.ForbiddenDirectories.Count);
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(testPath))
                System.IO.File.Delete(testPath);
        }
    }
    
    [Fact]
    public void AssemblyContext_UsesStrictPolicyWhenSpecified()
    {
        // Arrange
        string testPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(SecurityTests).Assembly.Location) ?? String.Empty, 
            "test.dll");
        
        // Create file for test
        System.IO.File.WriteAllText(testPath, "test");
        
        try
        {
            // Act
            using var context = new AssemblyContext(
                testPath, 
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Assert
            Assert.NotNull(context.SecurityPolicy);
            Assert.True(context.SecurityPolicy.StrictMode);
            Assert.True(context.SecurityPolicy.ForbiddenDirectories.Count > 
                AssemblySecurityPolicy.Default.ForbiddenDirectories.Count);
        }
        finally
        {
            // Cleanup
            if (System.IO.File.Exists(testPath))
                System.IO.File.Delete(testPath);
        }
    }
}