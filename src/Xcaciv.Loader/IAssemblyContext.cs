
namespace Xcaciv.Loader;

/// <summary>
/// class for managing dynamic assembly loading
/// </summary>
public interface IAssemblyContext : IDisposable
{
    /// <summary>
    /// full assembly file path
    /// </summary>
    string FilePath { get; }
    /// <summary>
    /// collect type instances for a base type
    /// </summary>
    /// <typeparam name="T">base type</typeparam>
    /// <returns></returns>
    IEnumerable<T> GetAllInstances<T>();
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class name.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    object? GetInstance(string className);
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class name.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    T GetInstance<T>(string className);
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
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    IEnumerable<Type> GetTypes<T>();
    /// <summary>
    /// return assembly version
    /// </summary>
    /// <returns></returns>
    Version GetVersion();
    /// <summary>
    /// attempt to unload the load context
    /// </summary>
    /// <returns></returns>
    bool Unload();
}
