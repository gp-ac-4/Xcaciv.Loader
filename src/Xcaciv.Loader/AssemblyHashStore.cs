using System;
using System.Collections.Generic;
using System.IO;

namespace Xcaciv.Loader;

/// <summary>
/// Stores and manages assembly file hashes for integrity verification.
/// Supports in-memory storage and CSV file persistence.
/// Thread-safe for concurrent access.
/// </summary>
/// <remarks>
/// Path Handling:
/// - Paths are treated as case-sensitive by the hash store (e.g., C:\Test\Assembly.dll and c:\test\assembly.dll are considered different entries).
///   However, note that Windows file systems are typically case-insensitive, so these paths may refer to the same file on disk.
///   This means that on Windows, using different casing for the same file path can result in duplicate or missing hash entries.
///   Users should ensure consistent casing for file paths when adding or retrieving hashes, especially on Windows.
/// - Relative paths are converted to absolute paths using Path.GetFullPath
/// - Path separators are normalized (forward slashes converted to backslashes on Windows)
/// - Paths with . and .. are resolved to their canonical form
/// 
/// CSV Format:
/// - Comment lines start with #
/// - Data format: FilePath,Hash
/// - Fields containing commas, quotes, or newlines are escaped per CSV standard
/// </remarks>
public class AssemblyHashStore
{
    private readonly Dictionary<string, string> hashes = new();
    private readonly object lockObject = new();
    
    /// <summary>
    /// Gets the number of hashes currently stored
    /// </summary>
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return hashes.Count;
            }
        }
    }
    
    /// <summary>
    /// Adds or updates a hash for the specified file path
    /// </summary>
    /// <param name="filePath">Full or relative path to the assembly file. Paths are case-sensitive.</param>
    /// <param name="hash">Base64-encoded hash value</param>
    /// <exception cref="ArgumentException">Thrown when filePath or hash is null or whitespace</exception>
    /// <remarks>
    /// The path will be normalized to an absolute path but case will be preserved.
    /// If a hash already exists for the normalized path, it will be updated.
    /// </remarks>
    public void AddOrUpdate(string filePath, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        ArgumentException.ThrowIfNullOrWhiteSpace(hash, nameof(hash));
        
        var normalizedPath = NormalizePath(filePath);
        
        lock (lockObject)
        {
            hashes[normalizedPath] = hash;
        }
    }
    
    /// <summary>
    /// Attempts to get the hash for the specified file path
    /// </summary>
    /// <param name="filePath">Full or relative path to the assembly file. Paths are case-sensitive.</param>
    /// <param name="hash">The hash value if found, null otherwise</param>
    /// <returns>True if hash was found, false otherwise</returns>
    /// <remarks>
    /// The path will be normalized to an absolute path before lookup.
    /// The lookup is case-sensitive, so the case must match the original stored path.
    /// </remarks>
    public bool TryGetHash(string filePath, out string? hash)
    {
        if (String.IsNullOrWhiteSpace(filePath))
        {
            hash = null;
            return false;
        }
        
        var normalizedPath = NormalizePath(filePath);
        
        lock (lockObject)
        {
            return hashes.TryGetValue(normalizedPath, out hash);
        }
    }
    
    /// <summary>
    /// Removes the hash for the specified file path
    /// </summary>
    /// <param name="filePath">Full or relative path to the assembly file. Paths are case-sensitive.</param>
    /// <returns>True if hash was removed, false if it didn't exist</returns>
    /// <remarks>
    /// The path will be normalized to an absolute path before removal.
    /// The lookup is case-sensitive, so the case must match the original stored path.
    /// </remarks>
    public bool Remove(string filePath)
    {
        if (String.IsNullOrWhiteSpace(filePath))
            return false;
        
        var normalizedPath = NormalizePath(filePath);
        
        lock (lockObject)
        {
            return hashes.Remove(normalizedPath);
        }
    }
    
    /// <summary>
    /// Removes all hashes from the store
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            hashes.Clear();
        }
    }
    
    /// <summary>
    /// Saves all hashes to a CSV file (comma-delimited: path,hash)
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
    /// <exception cref="IOException">Thrown when file cannot be written</exception>
    /// <remarks>
    /// File format: Each line contains "filepath,base64hash"
    /// Paths are written with their original case preserved.
    /// Paths and hashes are escaped for CSV compatibility.
    /// Comment lines are prefixed with # and are ignored during loading.
    /// </remarks>
    public void SaveToFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        Dictionary<string, string> snapshot;
        lock (lockObject)
        {
            snapshot = new Dictionary<string, string>(hashes);
        }
        
        using var writer = new StreamWriter(filePath, false);
        
        // Write header
        writer.WriteLine("# Assembly Integrity Hash Store");
        writer.WriteLine("# Format: FilePath,Hash");
        
        // Write each hash entry
        foreach (var kvp in snapshot)
        {
            var escapedPath = EscapeCsvField(kvp.Key);
            var escapedHash = EscapeCsvField(kvp.Value);
            writer.WriteLine($"{escapedPath},{escapedHash}");
        }
    }
    
    /// <summary>
    /// Loads hashes from a CSV file, replacing existing hashes
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <exception cref="FormatException">Thrown when file contains invalid format</exception>
    /// <remarks>
    /// Paths are loaded as-is from the file and then normalized to absolute paths.
    /// Case is preserved from the file. All existing hashes in the store are cleared before loading.
    /// Comment lines (starting with #) and empty lines are skipped.
    /// </remarks>
    public void LoadFromFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hash store file not found: {filePath}", filePath);
        }
        
        var loadedHashes = new Dictionary<string, string>();
        
        using (var reader = new StreamReader(filePath))
        {
            string? line;
            int lineNumber = 0;
            
            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;
                
                // Skip empty lines and comments
                if (String.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;
                
                var parts = ParseCsvLine(line);
                
                if (parts.Length != 2)
                {
                    throw new FormatException(
                        $"Invalid format at line {lineNumber}: Expected 'path,hash' but got {parts.Length} fields");
                }
                
                var path = parts[0].Trim();
                var hash = parts[1].Trim();
                
                if (String.IsNullOrWhiteSpace(path) || String.IsNullOrWhiteSpace(hash))
                {
                    throw new FormatException(
                        $"Invalid data at line {lineNumber}: Path and hash cannot be empty");
                }
                
                loadedHashes[path] = hash;
            }
        }
        
        lock (lockObject)
        {
            hashes.Clear();
            foreach (var kvp in loadedHashes)
            {
                hashes[kvp.Key] = kvp.Value;
            }
        }
    }
    
    /// <summary>
    /// Merges hashes from a CSV file with existing hashes
    /// </summary>
    /// <param name="filePath">Path to the CSV file</param>
    /// <param name="overwriteExisting">If true, file hashes overwrite existing ones; if false, existing hashes are preserved</param>
    /// <exception cref="ArgumentException">Thrown when filePath is null or whitespace</exception>
    /// <exception cref="FileNotFoundException">Thrown when file doesn't exist</exception>
    /// <exception cref="FormatException">Thrown when file contains invalid format</exception>
    /// <remarks>
    /// Paths are loaded as-is from the file and then normalized to absolute paths.
    /// Case is preserved from the file. Existing hashes are retained unless overwriteExisting is true.
    /// Comment lines (starting with #) and empty lines are skipped.
    /// Path matching is case-sensitive when checking for existing entries.
    /// </remarks>
    public void MergeFromFile(string filePath, bool overwriteExisting = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath, nameof(filePath));
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Hash store file not found: {filePath}", filePath);
        }
        
        var loadedHashes = new Dictionary<string, string>();
        
        using (var reader = new StreamReader(filePath))
        {
            string? line;
            int lineNumber = 0;
            
            while ((line = reader.ReadLine()) is not null)
            {
                lineNumber++;
                
                // Skip empty lines and comments
                if (String.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    continue;
                
                var parts = ParseCsvLine(line);
                
                if (parts.Length != 2)
                {
                    throw new FormatException(
                        $"Invalid format at line {lineNumber}: Expected 'path,hash' but got {parts.Length} fields");
                }
                
                var path = parts[0].Trim();
                var hash = parts[1].Trim();
                
                if (String.IsNullOrWhiteSpace(path) || String.IsNullOrWhiteSpace(hash))
                {
                    throw new FormatException(
                        $"Invalid data at line {lineNumber}: Path and hash cannot be empty");
                }
                
                loadedHashes[path] = hash;
            }
        }
        
        lock (lockObject)
        {
            foreach (var kvp in loadedHashes)
            {
                if (overwriteExisting || !hashes.ContainsKey(kvp.Key))
                {
                    hashes[kvp.Key] = kvp.Value;
                }
            }
        }
    }
    
    /// <summary>
    /// Gets all file paths that have stored hashes
    /// </summary>
    /// <returns>Collection of file paths with their original case preserved</returns>
    /// <remarks>
    /// Returns the normalized absolute paths as they are stored internally.
    /// The order of paths is not guaranteed.
    /// </remarks>
    public IReadOnlyCollection<string> GetFilePaths()
    {
        lock (lockObject)
        {
            return hashes.Keys.ToArray();
        }
    }
    
    /// <summary>
    /// Normalizes file path for consistent storage and lookup
    /// </summary>
    /// <remarks>
    /// Normalization includes:
    /// - Converting relative paths to absolute paths
    /// - Resolving . and .. path segments
    /// - Normalizing path separators (\ on Windows, / on Unix)
    /// - Case is preserved (paths are case-sensitive)
    /// </remarks>
    private static string NormalizePath(string filePath)
    {
        return Path.GetFullPath(filePath);
    }
    
    /// <summary>
    /// Escapes a field for CSV format (handles commas and quotes)
    /// </summary>
    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            // Escape quotes by doubling them and wrap in quotes
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
    
    /// <summary>
    /// Parses a CSV line handling quoted fields
    /// </summary>
    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var currentField = new System.Text.StringBuilder();
        bool inQuotes = false;
        
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // Double quote - add single quote to field
                    currentField.Append('"');
                    i++; // Skip next quote
                }
                else
                {
                    // Toggle quote mode
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // Field separator
                result.Add(currentField.ToString());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }
        
        // Add last field
        result.Add(currentField.ToString());
        
        return result.ToArray();
    }
}
