using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Loader;

/// <summary>
/// class for managing a single assembly dynamically loaded
/// </summary>
public class AssemblyContext : IAssemblyContext
{
    /// <summary>
    /// used by disposal
    /// </summary>
    private bool disposed;

    /// <summary>
    /// the directory path that the assembly is restricted to being loaded from
    /// </summary>
    public string BasePathRestriction { get; }

    /// <summary>
    /// full assembly file path
    /// </summary>
    public string FilePath { get; private set; }
    /// <summary>
    /// name for loading assembly
    /// </summary>
    private AssemblyName? name;

    /// <summary>
    /// instance for assembly loading
    /// </summary>
    private AssemblyLoadContext? loadContext = null;
    private bool isLoaded = false;

    /// <summary>
    /// create an instance of an assembly from a path
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="name"></param>
    /// <param name="isCollectible"></param>
    /// <param name="basePathRestriction">the directory path that the assembly is restricted to being loaded from</param>
    public AssemblyContext(string filePath, string? name = null, bool isCollectible = true, string basePathRestriction = ".")
    {
        if (String.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
        this.BasePathRestriction = basePathRestriction;
        this.FilePath = VerifyPath(filePath, this.BasePathRestriction);
        this.setContext(name, isCollectible);
    }

    /// <summary>
    /// create an instance of an assembly by unique identity
    /// </summary>
    /// <param name="assemblylName"></param>
    /// <param name="name"></param>
    /// <param name="isCollectible"></param>
    /// <param name="basePathRestriction">the directory path that the assembly is restricted to being loaded from</param>
    public AssemblyContext(AssemblyName assemblylName, string? name = null, bool isCollectible = true, string basePathRestriction = ".")
    {
        this.FilePath = String.Empty;
        this.BasePathRestriction = basePathRestriction;
        this.name = assemblylName;
        this.setContext(name, isCollectible);
    }

    private void setContext(string? name, bool isCollectible)
    {
        this.loadContext = new AssemblyLoadContext(name, isCollectible);
        this.loadContext.Resolving += LoadContext_Resolving;
        this.isLoaded = false;
    }

    private Assembly? LoadContext_Resolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (String.IsNullOrEmpty(this.FilePath)) return default;

        String filePath = Path.GetDirectoryName(this.FilePath) ?? String.Empty;
        var resolvedPath = (new AssemblyDependencyResolver(filePath)).ResolveAssemblyToPath(name);
        if (!String.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
        {
            return context.LoadFromAssemblyPath(resolvedPath);
        }

        String manualPath = Path.Combine(filePath, name.Name + ".dll");
        if (File.Exists(manualPath))
        {
            return context.LoadFromAssemblyPath(manualPath);
        }

        return default;
    }

    private Assembly? loadAssembly()
    {
        if (this.loadContext == null) throw new InvalidOperationException("Load context not set");

        if (this.isLoaded)
        {
            return this.loadContext.Assemblies.FirstOrDefault(o => o.FullName == this.name?.FullName);
        }

        if (!String.IsNullOrEmpty(this.FilePath))
        {
            var assembly = this.loadContext.LoadFromAssemblyPath(this.FilePath);
            if (assembly != null)
            {
                this.name = assembly.GetName();
                this.isLoaded = true;
                return assembly;
            }
        }
        else if (this.name != null)
        {
            var assembly = this.loadContext.LoadFromAssemblyName(this.name);
            if (assembly != null)
            {
                this.FilePath = VerifyPath(assembly.Location);
                this.isLoaded = true;
                return assembly;
            }
        }

        return default;

    }
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class name.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    public object? GetInstance(string className)
    {
        var assembly = this.loadAssembly();

        if (!className.Contains('.')) className = '.' + className;

        var instanceType = this.loadContext?.Assemblies.SelectMany(o => o.GetTypes()).FirstOrDefault(t => t.FullName?.EndsWith(className) == true);

        return (instanceType == null) ? null : Activator.CreateInstance(instanceType);
    }
    /// <summary>
    /// factory for creating a disposable context
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="name"></param>
    /// <param name="isCollectible"></param>
    /// <returns></returns>
    public static IAssemblyContext LoadFromPath(string filePath, string? name = null, bool isCollectible = true)
    {
        return new AssemblyContext(filePath, name, isCollectible, "*");
    }

    /// <summary>
    /// Attempts to create an instance from the current assembly given a class name.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public T GetInstance<T>(string className)
    {
        if (!className.Contains('.')) className = '.' + className;
        var instanceType = this.loadAssembly()?.GetTypes()?.FirstOrDefault(o => o.FullName?.EndsWith(className) == true);

        if (instanceType == null) throw new IndexOutOfRangeException(nameof(className));

        // Consumer is expected to handle any exceptions
        return GetInstance<T>(instanceType);
    }

    public static T GetInstance<T>(Type instanceType)
    {
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8603 // Possible null reference return.
        return (T)Activator.CreateInstance(instanceType);
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }

    /// <summary>
    /// collect type instances for a base type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IEnumerable<T> GetAllInstances<T>()
    {
        foreach (var type in this.GetTypes<T>().ToList())
        {
            yield return this.GetInstance<T>(type.FullName ?? String.Empty);
        }
    }
    /// <summary>
    /// list types from loaded assembly
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Type>? GetTypes()
    {
        return this.loadAssembly()?.GetTypes();
    }
    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public IEnumerable<Type>? GetTypes(Type baseType)
    {
        return this.loadAssembly()?.GetTypes().Where(o => baseType.IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract);
    }
    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public IEnumerable<Type> GetTypes<T>()
    {
        return this.loadAssembly()?.GetTypes().Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract) ?? new List<Type>();
    }

    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetLoadedTypes<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract) ?? new List<Type>();
    }

    /// <summary>
    /// return assembly version
    /// </summary>
    /// <returns></returns>
    public Version GetVersion()
    {
        return this.loadAssembly()?.GetName().Version ?? new Version();
    }
    /// <summary>
    /// translat to fully qualified file name
    /// with optional base path restriction
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="basePathRestriction"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static string VerifyPath(string filePath, string basePathRestriction = "*")
    {
        if (filePath == null) throw new ArgumentNullException(nameof(filePath));
        var fullFilePath = Path.GetFullPath(filePath);

        // Handle the lack of a base path restriction
        if (basePathRestriction == "*") basePathRestriction = Path.GetDirectoryName(fullFilePath) ?? String.Empty;
        // resolve base path restriction
        if (!String.IsNullOrEmpty(basePathRestriction)) basePathRestriction = Path.GetFullPath(basePathRestriction);
        // final check of base path restriction
        if (String.IsNullOrEmpty(basePathRestriction)) throw new ArgumentOutOfRangeException(nameof(filePath), $"Invalid base path restriction. ({basePathRestriction})");


        // make sure filePath is in the basePathRestriction path
        if (!fullFilePath.StartsWith(basePathRestriction)) throw new ArgumentOutOfRangeException(nameof(filePath), $"Path was not within the restricted path of {basePathRestriction}. ({fullFilePath})");

        return fullFilePath;
    }
    /// <summary>
    /// attempt to unload the load context
    /// </summary>
    /// <returns></returns>
    public bool Unload()
    {
        if (!this.loadContext?.IsCollectible ?? false || !this.isLoaded) return false;

        try
        {
            this.loadContext?.Unload();
            this.isLoaded = false;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("**" + ex.Message);
            return false;
        }
    }
    /// <summary>
    /// simple disposable implentation
    /// </summary>
    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.Unload();
        this.disposed = true;
    }
}
