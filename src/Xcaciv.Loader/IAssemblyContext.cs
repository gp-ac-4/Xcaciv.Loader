namespace Xcaciv.Loader;

/// <summary>
/// Interface for abstracting type loading
/// </summary>
public interface IAssemblyContext : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// full assembly file path
    /// </summary>
    string FilePath { get; }
    
    /// <summary>
    /// full assemblyName of the assembly for reference
    /// </summary>
    string FullAssemblyName { get; }
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class assemblyName.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    object? CreateInstance(string className);
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class assemblyName.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    T CreateInstance<T>(string className);
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given its Type and
    /// tries to box it to T
    /// If the class does not exist or cannot be boxed in T a null object is returned.
    /// </summary>
    /// <param name="classType"></param>
    /// <returns></returns>
    T CreateInstance<T>(Type classType);
    
    /// <summary>
    /// list types from loaded assembly
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type>? GetTypes();
    
    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    IEnumerable<Type>? GetTypes(Type baseType);
    
    /// <summary>
    /// list types that implement or extend a base type T
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type> GetTypes<T>();
    
    /// <summary>
    /// return assembly version
    /// </summary>
    /// <returns></returns>
    Version GetVersion();
    
    /// <summary>
    /// attempt to unload the load context
    /// May or may not be optimistic unloading
    /// </summary>
    /// <returns>true on success</returns>
    bool Unload();
    
    /// <summary>
    /// Asynchronously unloads the assembly context
    /// </summary>
    /// <returns>A task that completes when the unload operation is done</returns>
    Task<bool> UnloadAsync();

    /// <summary>
    /// Enables opt-in global monitoring of dynamic (in-memory) assemblies created
    /// anywhere in the AppDomain. When enabled, this context will raise
    /// <c>SecurityViolation</c> events when a dynamic assembly is observed and the
    /// context's security policy disallows dynamic assemblies.
    /// </summary>
    /// <remarks>
    /// This is an audit-only mechanism; global monitoring cannot prevent emit operations
    /// and does not throw. For enforcement, the context already blocks dynamic assemblies
    /// it loads when <c>DisallowDynamicAssemblies</c> is enabled.
    /// </remarks>
    void EnableGlobalDynamicAssemblyMonitoring();
}
