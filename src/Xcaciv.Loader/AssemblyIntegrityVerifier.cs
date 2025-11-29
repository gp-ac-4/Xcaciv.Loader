using System;
using System.IO;
using System.Security;
using System.Security.Cryptography;

namespace Xcaciv.Loader;

/// <summary>
/// Verifies assembly file integrity using cryptographic hash comparison.
/// Supports automatic hash learning and persistence for trusted assemblies.
/// </summary>
/// <remarks>
/// <para>This class provides defense-in-depth protection against tampered assemblies.</para>
/// <para>When enabled, assemblies are verified against known hashes before loading.</para>
/// <para>In learning mode, new assembly hashes are automatically stored for future verification.</para>
/// </remarks>
public class AssemblyIntegrityVerifier
{
    private readonly AssemblyHashStore hashStore;
    private readonly HashAlgorithmName algorithmName;
    private readonly bool learningMode;
    
    /// <summary>
    /// Gets whether integrity verification is enabled
    /// </summary>
    public bool Enabled { get; init; }
    
    /// <summary>
    /// Gets whether learning mode is enabled (automatically store new hashes)
    /// </summary>
    public bool LearningMode => learningMode;
    
    /// <summary>
    /// Gets the hash algorithm being used
    /// </summary>
    public HashAlgorithmName Algorithm => algorithmName;
    
    /// <summary>
    /// Gets the hash store managing assembly hashes
    /// </summary>
    public AssemblyHashStore HashStore => hashStore;
    
    /// <summary>
    /// Event raised when a hash mismatch is detected
    /// Parameters: (filePath, expectedHash, actualHash)
    /// </summary>
    public event Action<string, string, string>? HashMismatchDetected;
    
    /// <summary>
    /// Event raised when a new assembly hash is learned
    /// Parameters: (filePath, hash)
    /// </summary>
    public event Action<string, string>? HashLearned;
    
    /// <summary>
    /// Creates a new disabled integrity verifier (no verification performed)
    /// </summary>
    /// <remarks>
    /// This is the default configuration. Verification must be explicitly enabled.
    /// </remarks>
    public AssemblyIntegrityVerifier()
    {
        this.Enabled = false;
        this.learningMode = false;
        this.algorithmName = HashAlgorithmName.SHA256;
        this.hashStore = new AssemblyHashStore();
    }
    
    /// <summary>
    /// Creates a new integrity verifier with specified configuration
    /// </summary>
    /// <param name="enabled">Whether to enable integrity verification</param>
    /// <param name="learningMode">Whether to automatically learn and store new assembly hashes</param>
    /// <param name="algorithm">Hash algorithm to use (default: SHA256)</param>
    /// <param name="hashStore">Optional pre-configured hash store; if null, creates a new empty store</param>
    /// <example>
    /// <code>
    /// // Create verifier in learning mode
    /// var verifier = new AssemblyIntegrityVerifier(
    ///     enabled: true,
    ///     learningMode: true,
    ///     algorithm: HashAlgorithmName.SHA256);
    /// 
    /// // Create verifier with pre-loaded hashes
    /// var store = new AssemblyHashStore();
    /// store.LoadFromFile("trusted-hashes.json");
    /// var verifier = new AssemblyIntegrityVerifier(
    ///     enabled: true,
    ///     learningMode: false,
    ///     hashStore: store);
    /// </code>
    /// </example>
    public AssemblyIntegrityVerifier(
        bool enabled, 
        bool learningMode = true, 
        HashAlgorithmName algorithm = default,
        AssemblyHashStore? hashStore = null)
    {
        this.Enabled = enabled;
        this.learningMode = learningMode;
        this.algorithmName = algorithm == default ? HashAlgorithmName.SHA256 : algorithm;
        this.hashStore = hashStore ?? new AssemblyHashStore();
    }
    
    /// <summary>
    /// Verifies the integrity of an assembly file
    /// </summary>
    /// <param name="filePath">Full path to the assembly file</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <exception cref="SecurityException">Thrown when hash verification fails</exception>
    /// <remarks>
    /// <para><strong>Behavior:</strong></para>
    /// <list type="bullet">
    ///   <item><description>If disabled: Returns immediately without verification</description></item>
    ///   <item><description>If enabled and hash exists: Verifies against stored hash</description></item>
    ///   <item><description>If enabled, learning mode, and no hash: Learns and stores the hash</description></item>
    ///   <item><description>If enabled, not learning, and no hash: Throws SecurityException</description></item>
    /// </list>
    /// </remarks>
    public void VerifyIntegrity(string filePath)
    {
        if (!Enabled)
            return;
        
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Assembly file not found: {filePath}", filePath);
        }
        
        var actualHash = ComputeHash(filePath);
        
        if (hashStore.TryGetHash(filePath, out var expectedHash))
        {
            // Hash exists - verify it matches
            if (!String.Equals(expectedHash, actualHash, StringComparison.Ordinal))
            {
                HashMismatchDetected?.Invoke(filePath, expectedHash!, actualHash);
                throw new SecurityException(
                    $"Assembly integrity verification failed for: {filePath}\n" +
                    $"Expected hash: {expectedHash}\n" +
                    $"Actual hash:   {actualHash}");
            }
        }
        else
        {
            // No hash exists
            if (learningMode)
            {
                // Learning mode - store the hash for future verification
                hashStore.AddOrUpdate(filePath, actualHash);
                HashLearned?.Invoke(filePath, actualHash);
            }
            else
            {
                // Not learning mode - reject unknown assemblies
                throw new SecurityException(
                    $"No trusted hash found for assembly: {filePath}\n" +
                    $"Computed hash: {actualHash}\n" +
                    $"Enable learning mode to automatically trust new assemblies, or add the hash manually.");
            }
        }
    }
    
    /// <summary>
    /// Computes the hash of a file
    /// </summary>
    /// <param name="filePath">Full path to the file</param>
    /// <returns>Base64-encoded hash</returns>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <exception cref="IOException">Thrown when file cannot be read</exception>
    public string ComputeHash(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}", filePath);
        }
        
        using var stream = File.OpenRead(filePath);
        
        byte[] hashBytes = algorithmName.Name switch
        {
            "SHA256" => SHA256.HashData(stream),
            "SHA384" => SHA384.HashData(stream),
            "SHA512" => SHA512.HashData(stream),
            _ => throw new NotSupportedException($"Hash algorithm not supported: {algorithmName.Name}")
        };
        
        return Convert.ToBase64String(hashBytes);
    }
    
    /// <summary>
    /// Pre-computes and stores the hash for an assembly
    /// </summary>
    /// <param name="filePath">Full path to the assembly file</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <remarks>
    /// Use this method to pre-populate the hash store with trusted assemblies.
    /// </remarks>
    public void TrustAssembly(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        var hash = ComputeHash(filePath);
        hashStore.AddOrUpdate(filePath, hash);
    }
    
    /// <summary>
    /// Removes trust for an assembly
    /// </summary>
    /// <param name="filePath">Full path to the assembly file</param>
    /// <returns>True if the hash was removed, false if it didn't exist</returns>
    public bool UntrustAssembly(string filePath)
    {
        if (String.IsNullOrWhiteSpace(filePath))
            return false;
        
        return hashStore.Remove(filePath);
    }
    
    /// <summary>
    /// Checks if an assembly is trusted (has a stored hash)
    /// </summary>
    /// <param name="filePath">Full path to the assembly file</param>
    /// <returns>True if the assembly has a stored hash, false otherwise</returns>
    public bool IsTrusted(string filePath)
    {
        if (String.IsNullOrWhiteSpace(filePath))
            return false;
        
        return hashStore.TryGetHash(filePath, out _);
    }
}
