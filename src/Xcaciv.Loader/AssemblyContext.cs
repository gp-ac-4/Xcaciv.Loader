using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Xcaciv.Loader;

/// <summary>
/// class for managing a single assembly dynamically loaded and optimistically 
/// unloaded
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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
    private AssemblyName? assemblyName;
    /// <summary>
    /// string name for refrence
    /// </summary>
    public string FullAssemblyName { get { return this.assemblyName?.FullName ?? String.Empty; } }
    /// <summary>
    /// instance for assembly loading
    /// </summary>
    private AssemblyLoadContext? loadContext = null;
    /// <summary>
    /// indicator that assembly is loaded
    /// </summary>
    private bool isLoaded = false;
    /// <summary>
    /// instance of assembly reflection
    /// </summary>
    private Assembly? assembly;

    /// <summary>
    /// create a new AssemblyContext abstraction instance
    /// </summary>
    /// <param name="filePath"></param>
    /// <param name="fullName"></param>
    /// <param name="isCollectible"></param>
    /// <param name="basePathRestriction">the directory path that the assembly is restricted to being loaded from</param>
    /// <exception cref="ArgumentNullException"></exception>
    public AssemblyContext(string filePath, string? fullName = null, bool isCollectible = true, string basePathRestriction = ".")
    {
        if (String.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath), "Assembly file path cannot be null or empty");
        this.BasePathRestriction = basePathRestriction;
        this.FilePath = VerifyPath(filePath, this.BasePathRestriction);
        this.setLoadContext(fullName ?? String.Empty, isCollectible);
    }

    /// <summary>
    /// create a new AssemblyContext abstraction instance
    /// </summary>
    /// <param name="assemblylName"></param>
    /// <param name="isCollectible"></param>
    /// <param name="basePathRestriction">the directory path that the assembly is restricted to being loaded from</param>
    public AssemblyContext(AssemblyName assemblylName, bool isCollectible = true, string basePathRestriction = ".") 
    {
        this.FilePath = String.Empty;
        this.BasePathRestriction = basePathRestriction;
        this.assemblyName = assemblylName ?? throw new ArgumentNullException(nameof(assemblylName), "Assembly name cannot be null");
        this.setLoadContext(this.assemblyName.FullName, isCollectible);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="fullName"></param>
    /// <param name="isCollectible"></param>
    protected void setLoadContext(string fullName, bool isCollectible) 
    {
        loadContext = new AssemblyLoadContext(fullName, isCollectible);
        this.loadContext.Resolving += LoadContext_Resolving;
        this.isLoaded = false;
    }
    
    /// <summary>
    /// resolve assembly when not immediately found (not folder adjacent or GAC)
    /// </summary>
    /// <param name="context"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private Assembly? LoadContext_Resolving(AssemblyLoadContext context, AssemblyName name)
    {
        if (String.IsNullOrEmpty(this.FilePath)) return default;
        String assemblyPath = String.Empty;

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
        
    /// <summary>
    /// Indicates whether the load context has been initialized
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the load context is not set</exception>
    protected void ValidateLoadContext()
    {
        if (this.loadContext == null)
        {
            throw new InvalidOperationException("Load context is not set. Make sure initialization was completed successfully.");
        }
    }
    
    /// <summary>
    /// Loads an assembly from its file path
    /// </summary>
    /// <returns>Loaded assembly or null if loading failed</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file cannot be found</exception>
    /// <exception cref="BadImageFormatException">Thrown when the file is not a valid assembly</exception>
    protected Assembly? LoadFromPath()
    {
        ValidateLoadContext();
        
        if (String.IsNullOrEmpty(this.FilePath))
        {
            throw new InvalidOperationException("Cannot load assembly: File path is not set");
        }

        if (!File.Exists(this.FilePath))
        {
            throw new FileNotFoundException($"Assembly file not found at specified path", this.FilePath);
        }
        
        try
        {
            Assembly? loadedAssembly = this.loadContext!.LoadFromAssemblyPath(this.FilePath);

            if (loadedAssembly != null)
            {
                this.assemblyName = loadedAssembly.GetName();
                this.isLoaded = true;
            }
            return loadedAssembly;
        }
        catch (FileLoadException ex)
        {
            throw new FileLoadException($"Failed to load assembly from path: {this.FilePath}", ex);
        }
        catch (BadImageFormatException ex)
        {
            throw new BadImageFormatException($"The file at path '{this.FilePath}' is not a valid assembly", ex);
        }
    }
    
    /// <summary>
    /// Loads an assembly by its name
    /// </summary>
    /// <returns>Loaded assembly or null if loading failed</returns>
    /// <exception cref="FileNotFoundException">Thrown when the assembly cannot be found</exception>
    /// <exception cref="ArgumentException">Thrown when the assembly name is invalid</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic assembly loading is intrinsic to this library")]
    protected Assembly? LoadFromName()
    {
        ValidateLoadContext();
        
        if (this.assemblyName == null)
        {
            throw new InvalidOperationException("Cannot load assembly: Assembly name is not set");
        }
        
        try
        {
            Assembly? loadedAssembly = this.loadContext!.LoadFromAssemblyName(this.assemblyName);
            if (loadedAssembly != null)
            {
                this.FilePath = VerifyPath(loadedAssembly.Location);
                this.isLoaded = true;
            }
            return loadedAssembly;
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException($"Assembly '{this.assemblyName.FullName}' could not be found", ex);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException($"Invalid assembly name: {this.assemblyName.FullName}", ex);
        }
    }
    
    /// <summary>
    /// use the framework's AssemblyLoadContext to provide loading of assembly that 
    /// can be unloaded later
    /// </summary>
    /// <returns>null if we did not find the assembly</returns>
    /// <exception cref="InvalidOperationException">Thrown when load context is not set or when assembly cannot be loaded</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file cannot be found</exception>
    /// <exception cref="BadImageFormatException">Thrown when the file is not a valid assembly</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic assembly loading is intrinsic to this library")]
    protected Assembly? loadAssembly()
    {
        ValidateLoadContext();

        // this assembly is already loaded
        if (assembly != null)
        {
            return assembly;
        }
        else if (this.isLoaded) // the assembly may be in memory already
        {
            assembly = this.loadContext!.Assemblies.FirstOrDefault(o => o.FullName == this.assemblyName?.FullName);
            this.assemblyName = assembly?.GetName();
            return assembly;
        }

        if (!String.IsNullOrEmpty(this.FilePath))
        {
            assembly = LoadFromPath();
        }
        else if (this.assemblyName != null)
        {
            assembly = LoadFromName();
        }
        else
        {
            throw new InvalidOperationException("Cannot load assembly: Neither file path nor assembly name is set");
        }

        this.assemblyName = assembly?.GetName();
        return assembly;
    }
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class assemblyName.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className">The name of the class to instantiate</param>
    /// <returns>A refrence to the newly created object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the assembly cannot be loaded</exception>
    /// <exception cref="FileNotFoundException">Thrown when the assembly file doesn't exist</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type loading is intrinsic to this library")]
    public object? CreateInstance(string className)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentNullException(nameof(className), "Class name cannot be null or empty");
        }
        
        if (!String.IsNullOrEmpty(this.FilePath) && !File.Exists(this.FilePath))
        {
            throw new FileNotFoundException("Assembly file not found at specified path", this.FilePath);
        }

        try
        {
            var assembly = this.loadAssembly();
            if (assembly == null)
            {
                return null;
            }

            if (!className.Contains('.')) className = '.' + className;

            var instanceType = this.loadContext?.Assemblies.SelectMany(o => o.GetTypes()).FirstOrDefault(t => t.FullName?.EndsWith(className) == true);

            return (instanceType == null) ? null : Activator.CreateInstance(instanceType);
        }
        catch (FileNotFoundException)
        {
            throw; // Rethrow file not found exceptions directly
        }
        catch (ReflectionTypeLoadException ex)
        {
            throw new InvalidOperationException($"Failed to load types from assembly: {ex.Message}", ex);
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            throw new InvalidOperationException($"Failed to create instance of '{className}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class name.
    /// If the class does not exist in this assembly a null object is returned.
    /// </summary>
    /// <param name="className">The name of the class to instantiate</param>
    /// <returns>A refrence to the newly created object</returns>
    /// <exception cref="InvalidOperationException">Thrown when the assembly cannot be loaded</exception>
    /// <exception cref="TypeNotFoundException">Thrown when the specified class type is not found</exception>
    /// <exception cref="TypeLoadException">Thrown when there's an error loading the type</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type loading is intrinsic to this library")]
    public T CreateInstance<T>(string className)
    {
        if (string.IsNullOrEmpty(className))
        {
            throw new ArgumentNullException(nameof(className), "Class name cannot be null or empty");
        }
        
        if (!String.IsNullOrEmpty(this.FilePath) && !File.Exists(this.FilePath))
        {
            throw new FileNotFoundException("Assembly file not found at specified path", this.FilePath);
        }

        try
        {
            if (!className.Contains('.')) className = '.' + className;
            
            var assembly = this.loadAssembly();
            if (assembly == null)
            {
                throw new InvalidOperationException($"Failed to load assembly for creating instance of '{className}'");
            }
            
            var instanceType = assembly.GetTypes()?.FirstOrDefault(o => o.FullName?.EndsWith(className) == true);

            if (instanceType == null)
            {
                throw new TypeNotFoundException(className, assembly.FullName ?? "Unknown Assembly");
            }

            // Consumer is expected to handle any exceptions
            return ActivateInstance<T>(instanceType);
        }
        catch (FileNotFoundException)
        {
            throw; // Rethrow file not found exceptions directly
        }
        catch (ReflectionTypeLoadException ex)
        {
            throw new TypeLoadException($"Failed to load types from assembly: {ex.Message}", ex);
        }
        catch (TypeNotFoundException)
        {
            throw; // Re-throw the TypeNotFoundException we created
        }
        catch (InvalidCastException ex)
        {
            throw new InvalidCastException($"Cannot convert instance of {className} to type {typeof(T).FullName}", ex);
        }
        catch (Exception ex) when (ex is not ArgumentNullException && ex is not InvalidOperationException && ex is not TypeLoadException)
        {
            throw new InvalidOperationException($"Failed to create instance of '{className}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Attempts to create an instance from the current assembly given a class Type.
    /// If the class does not exist in this assembly a null object is returned
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instanceType"></param>
    /// <returns>A refrence to the newly created object</returns>
    /// <exception cref="ArgumentNullException">Thrown when instanceType is null</exception>
    /// <exception cref="InvalidCastException">Thrown when the created object cannot be cast to T</exception>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type activation is intrinsic to this library")]
    public T CreateInstance<T>(Type instanceType)
    {
        if (instanceType == null) throw new ArgumentNullException(nameof(instanceType), "Instance type cannot be null");

        try
        {
            // Consumer is expected to handle any exceptions
            return ActivateInstance<T>(instanceType);
        }
        catch (InvalidCastException ex)
        {
            throw new InvalidCastException($"Cannot convert instance of {instanceType.FullName} to type {typeof(T).FullName}", ex);
        }
        catch (Exception ex) when (ex is not ArgumentNullException)
        {
            throw new InvalidOperationException($"Failed to create instance of '{instanceType.FullName}': {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// use the activator
    /// compartmentalizes the call for exception/suppression
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="instanceType"></param>
    /// <returns>A refrence to the newly created object</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type activation is intrinsic to this library")]
    public static T ActivateInstance<T>(Type instanceType)
    {
        ArgumentNullException.ThrowIfNull(instanceType);
        
        object? instance = Activator.CreateInstance(instanceType);
        if (instance is null)
        {
            throw new InvalidOperationException($"Failed to create instance of {instanceType.FullName}");
        }
        
        return (T)instance;
    }

    /// <summary>
    /// list types from loaded assembly
    /// </summary>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type discovery is intrinsic to this library")]
    public IEnumerable<Type>? GetTypes()
    {
        return this.loadAssembly()?.GetTypes();
    }
    
    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <param name="baseType"></param>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type discovery is intrinsic to this library")]
    public IEnumerable<Type>? GetTypes(Type baseType)
    {
        return this.loadAssembly()?.GetTypes()?.Where(o => baseType.IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract);
    }
    
    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type discovery is intrinsic to this library")]
    public IEnumerable<Type> GetTypes<T>()
    {
        return this.loadAssembly()?.GetTypes()?.Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract) ?? Enumerable.Empty<Type>();
    }

    /// <summary>
    /// list types that implement or extend a base type
    /// </summary>
    /// <returns></returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Dynamic type discovery is intrinsic to this library")]
    public static IEnumerable<Type> GetLoadedTypes<T>()
    {
        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(o => typeof(T).IsAssignableFrom(o) && !o.IsInterface && !o.IsAbstract) ?? Enumerable.Empty<Type>();
    }

    /// <summary>
    /// return assembly version
    /// </summary>
    /// <returns></returns>
    public Version GetVersion()
    {
        return this.loadAssembly()?.GetName()?.Version ?? new Version();
    }
    
    /// <summary>
    /// translat to fully qualified file assemblyName
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
            this.assembly = null;
            var context = this.loadContext;
            this.loadContext = null;
            context?.Unload();

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
