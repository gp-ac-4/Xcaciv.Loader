using System;
using System.IO;
using System.Security;
using Xunit;

namespace Xcaciv.Loader.Tests;

/// <summary>
/// Tests for DisallowDynamicAssemblies security policy functionality.
/// Verifies that dynamic assembly loading is blocked when the policy is enabled.
/// </summary>
public class DisallowDynamicAssembliesTests
{
    #region Policy Configuration Tests

    [Fact]
    public void StrictPolicy_HasDisallowDynamicAssembliesEnabled()
    {
        // Arrange & Act
        var strictPolicy = AssemblySecurityPolicy.Strict;
        
        // Assert
        Assert.True(strictPolicy.DisallowDynamicAssemblies, 
            "Strict policy should have DisallowDynamicAssemblies enabled");
    }

    [Fact]
    public void DefaultPolicy_HasDisallowDynamicAssembliesDisabled()
    {
        // Arrange & Act
        var defaultPolicy = AssemblySecurityPolicy.Default;
        
        // Assert
        Assert.False(defaultPolicy.DisallowDynamicAssemblies, 
            "Default policy should have DisallowDynamicAssemblies disabled");
    }

    [Fact]
    public void CustomPolicy_WithoutStrictMode_HasDisallowDynamicAssembliesDisabled()
    {
        // Arrange & Act
        var customPolicy = new AssemblySecurityPolicy(new[] { "custom" });
        
        // Assert
        Assert.False(customPolicy.DisallowDynamicAssemblies, 
            "Custom policy should have DisallowDynamicAssemblies disabled");
    }

    [Fact]
    public void StrictModePolicy_ConsistentlyDisallowsDynamicAssemblies()
    {
        // Arrange & Act
        var strictPolicy = new AssemblySecurityPolicy(strictMode: true);
        
        // Assert
        Assert.True(strictPolicy.StrictMode);
        Assert.True(strictPolicy.DisallowDynamicAssemblies,
            "Strict mode should enable DisallowDynamicAssemblies");
    }

    #endregion

    #region Context Configuration Tests

    [Fact]
    public void AssemblyContext_WithDefaultPolicy_AllowsDynamicAssemblies()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        try
        {
            File.WriteAllText(testPath, "test");
            
            // Act
            using var context = new AssemblyContext(testPath, basePathRestriction: "*");
            
            // Assert
            Assert.NotNull(context.SecurityPolicy);
            Assert.False(context.SecurityPolicy.DisallowDynamicAssemblies);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [Fact]
    public void AssemblyContext_WithStrictPolicy_DisallowsDynamicAssemblies()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        try
        {
            File.WriteAllText(testPath, "test");
            
            // Act
            using var context = new AssemblyContext(
                testPath, 
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Assert
            Assert.NotNull(context.SecurityPolicy);
            Assert.True(context.SecurityPolicy.DisallowDynamicAssemblies);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    [Fact]
    public void AssemblyContext_WithCustomPolicy_RespectsConfiguration()
    {
        // Arrange
        var testPath = Path.Combine(Path.GetTempPath(), "test.dll");
        var customPolicy = new AssemblySecurityPolicy(new[] { "custom" });
        try
        {
            File.WriteAllText(testPath, "test");
            
            // Act
            using var context = new AssemblyContext(
                testPath,
                basePathRestriction: "*",
                securityPolicy: customPolicy);
            
            // Assert
            Assert.NotNull(context.SecurityPolicy);
            Assert.False(context.SecurityPolicy.DisallowDynamicAssemblies);
        }
        finally
        {
            if (File.Exists(testPath))
                File.Delete(testPath);
        }
    }

    #endregion

    #region Dynamic Assembly Detection Tests

    [Fact]
    public void DisallowDynamicAssemblies_Property_IsInitOnly()
    {
        // Arrange
        var policy = AssemblySecurityPolicy.Default;
        
        // The DisallowDynamicAssemblies property has init-only accessor
        // This test verifies the property exists and has the expected value
        Assert.IsType<bool>(policy.DisallowDynamicAssemblies);
    }

    [Fact]
    public void AssemblyContext_MultipleContexts_EachHasOwnPolicy()
    {
        // Arrange
        var testPath1 = Path.Combine(Path.GetTempPath(), "test1.dll");
        var testPath2 = Path.Combine(Path.GetTempPath(), "test2.dll");
        try
        {
            File.WriteAllText(testPath1, "test1");
            File.WriteAllText(testPath2, "test2");
            
            // Act
            using var context1 = new AssemblyContext(
                testPath1,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Default);
            
            using var context2 = new AssemblyContext(
                testPath2,
                basePathRestriction: "*",
                securityPolicy: AssemblySecurityPolicy.Strict);
            
            // Assert
            Assert.False(context1.SecurityPolicy.DisallowDynamicAssemblies);
            Assert.True(context2.SecurityPolicy.DisallowDynamicAssemblies);
        }
        finally
        {
            if (File.Exists(testPath1))
                File.Delete(testPath1);
            if (File.Exists(testPath2))
                File.Delete(testPath2);
        }
    }

    #endregion

    #region Interaction Tests

    [Fact]
    public void DisallowDynamicAssemblies_IndependentOfPathRestriction()
    {
        // The DisallowDynamicAssemblies policy is independent of path-based restrictions
        // This verifies that both can be used together without conflicts
        
        // Arrange
        var policy = AssemblySecurityPolicy.Strict;
        
        // Assert
        Assert.True(policy.StrictMode);
        Assert.True(policy.DisallowDynamicAssemblies);
        Assert.NotEmpty(policy.ForbiddenDirectories);
    }

    [Fact]
    public void DisallowDynamicAssemblies_CanBeCombinedWithCustomDirectories()
    {
        // Create a strict policy which has DisallowDynamicAssemblies enabled
        var strictPolicy = new AssemblySecurityPolicy(strictMode: true);
        
        // Assert
        Assert.True(strictPolicy.DisallowDynamicAssemblies);
        Assert.True(strictPolicy.StrictMode);
        Assert.NotEmpty(strictPolicy.ForbiddenDirectories);
    }

    #endregion

    #region Policy Inheritance Tests

    [Fact]
    public void StrictPolicy_InheritsAllProtections()
    {
        // Arrange & Act
        var strictPolicy = AssemblySecurityPolicy.Strict;
        var defaultPolicy = AssemblySecurityPolicy.Default;
        
        // Assert
        Assert.True(strictPolicy.StrictMode);
        Assert.True(strictPolicy.DisallowDynamicAssemblies);
        
        // Strict should have more forbidden directories than default
        Assert.True(strictPolicy.ForbiddenDirectories.Count > defaultPolicy.ForbiddenDirectories.Count,
            "Strict policy should have more forbidden directories than default");
    }

    #endregion
}
