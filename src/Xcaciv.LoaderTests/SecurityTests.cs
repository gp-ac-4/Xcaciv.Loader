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
        // Enable strict mode
        AssemblyContext.SetStrictDirectoryRestriction(true);
        
        try
        {
            // This should throw a SecurityException because it points to a system directory
            string testPath = @"C:\Windows\System32\kernel32.dll";
            
            var exception = Assert.Throws<SecurityException>(() => 
                AssemblyContext.VerifyPath(testPath));
            
            Assert.Contains("system directories", exception.Message, StringComparison.OrdinalIgnoreCase);
            
            // Verify strict mode is enabled
            Assert.True(AssemblyContext.IsStrictDirectoryRestrictionEnabled());
        }
        finally
        {
            // Reset to default mode for other tests
            AssemblyContext.SetStrictDirectoryRestriction(false);
        }
    }
    
    [Fact]
    public void DefaultMode_ChecksBasicRestrictions()
    {
        // Ensure default mode is active
        AssemblyContext.SetStrictDirectoryRestriction(false);
        
        // Verify default mode can still validate paths
        string validPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(SecurityTests).Assembly.Location) ?? string.Empty, 
            "test.dll");
            
        string result = AssemblyContext.VerifyPath(validPath);
        
        Assert.NotNull(result);
        Assert.False(AssemblyContext.IsStrictDirectoryRestrictionEnabled());
    }
}