using System;

namespace Xcaciv.Loader.Exceptions;

/// <summary>
/// Exception thrown when a requested type cannot be found in an assembly
/// </summary>
public class TypeNotFoundException : Exception
{
    /// <summary>
    /// The name of the type that was not found
    /// </summary>
    public string TypeName { get; }
    
    /// <summary>
    /// The name of the assembly where the type was expected to be found
    /// </summary>
    public string AssemblyName { get; }
    
    /// <summary>
    /// Creates a new instance of TypeNotFoundException
    /// </summary>
    /// <param name="typeName">The name of the type that was not found</param>
    /// <param name="assemblyName">The name of the assembly where the type was expected to be found</param>
    public TypeNotFoundException(string typeName, string assemblyName) 
        : base($"Type '{typeName}' was not found in assembly '{assemblyName}'")
    {
        TypeName = typeName;
        AssemblyName = assemblyName;
    }
    
    /// <summary>
    /// Creates a new instance of TypeNotFoundException
    /// </summary>
    /// <param name="typeName">The name of the type that was not found</param>
    /// <param name="assemblyName">The name of the assembly where the type was expected to be found</param>
    /// <param name="innerException">The exception that is the cause of the current exception</param>
    public TypeNotFoundException(string typeName, string assemblyName, Exception innerException) 
        : base($"Type '{typeName}' was not found in assembly '{assemblyName}'", innerException)
    {
        TypeName = typeName;
        AssemblyName = assemblyName;
    }
}