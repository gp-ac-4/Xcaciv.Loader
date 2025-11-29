using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Xcaciv.Loader;

/// <summary>
/// Utility for scanning and discovering types across loaded assemblies in the current AppDomain.
/// </summary>
/// <remarks>
/// This class provides convenient methods for type discovery and filtering based on inheritance
/// and implementation relationships. Use these methods when you need to discover types that
/// implement specific interfaces or inherit from specific base classes.
/// </remarks>
public static class AssemblyScanner
{
    /// <summary>
    /// Gets all loaded types in the current AppDomain that implement or extend type T.
    /// Excludes interfaces and abstract classes from the results.
    /// </summary>
    /// <typeparam name="T">The base type or interface to search for</typeparam>
    /// <returns>
    /// Collection of concrete types that implement or extend T. Returns empty collection if no matches found.
    /// </returns>
    /// <remarks>
    /// <para>This method scans all assemblies currently loaded in the AppDomain.</para>
    /// <para><strong>Performance:</strong> This operation can be expensive as it scans all loaded assemblies and their types.</para>
    /// <para><strong>Thread Safety:</strong> This method is thread-safe but the AppDomain's loaded assemblies may change between calls.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Find all loaded plugin types
    /// var plugins = AssemblyScanner.GetLoadedTypes&lt;IPlugin&gt;();
    /// foreach (var pluginType in plugins)
    /// {
    ///     var plugin = Activator.CreateInstance(pluginType) as IPlugin;
    ///     plugin?.Initialize();
    /// }
    /// </code>
    /// </example>
    public static IEnumerable<Type> GetLoadedTypes<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => GetTypesFromAssembly(assembly))
            .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
    }
    
    /// <summary>
    /// Gets types from a specific assembly that implement or extend type T.
    /// Excludes interfaces and abstract classes from the results.
    /// </summary>
    /// <typeparam name="T">The base type or interface to search for</typeparam>
    /// <param name="assembly">The assembly to scan for types</param>
    /// <returns>
    /// Collection of concrete types from the specified assembly that implement or extend T.
    /// Returns empty collection if no matches found.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when assembly is null</exception>
    /// <remarks>
    /// <para>Use this method when you have a specific assembly to scan, such as a dynamically loaded plugin.</para>
    /// <para><strong>Performance:</strong> More efficient than scanning all loaded assemblies.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// using var context = new AssemblyContext(pluginPath);
    /// var assembly = context.LoadAssembly();
    /// var handlers = AssemblyScanner.GetTypes&lt;IMessageHandler&gt;(assembly);
    /// </code>
    /// </example>
    public static IEnumerable<Type> GetTypes<T>(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly, nameof(assembly));
        
        return GetTypesFromAssembly(assembly)
            .Where(type => typeof(T).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract);
    }
    
    /// <summary>
    /// Gets types from a specific assembly, filtering out compiler-generated types.
    /// </summary>
    /// <param name="assembly">The assembly to get types from</param>
    /// <returns>Collection of types from the assembly</returns>
    /// <remarks>
    /// This method safely handles ReflectionTypeLoadException by returning only successfully loaded types.
    /// </remarks>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type discovery is intrinsic to this library")]
    private static IEnumerable<Type> GetTypesFromAssembly(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            // Return only the types that loaded successfully
            return ex.Types.Where(type => type is not null)!;
        }
    }
}
