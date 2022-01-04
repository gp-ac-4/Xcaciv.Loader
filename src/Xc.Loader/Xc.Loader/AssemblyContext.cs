using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Threading.Tasks;

namespace Xc.Loader
{
    /// <summary>
    /// class for managing a single assembly dynamically loaded
    /// </summary>
    public class AssemblyContext : IDisposable
    {
        private bool disposed;

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
        private AssemblyLoadContext loadContext;
        private bool isLoaded;

        /// <summary>
        /// create an instance of an assembly from a path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isCollectible"></param>
        public AssemblyContext(string filePath, string? name = null, bool isCollectible = true)
        {
            if (String.IsNullOrEmpty(filePath)) throw new ArgumentNullException(nameof(filePath));
            this.FilePath = VerifyPath(filePath);
            this.setContext(name, isCollectible);
        }
        /// <summary>
        /// create an instance of an assembly by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isCollectible"></param>
        public AssemblyContext(AssemblyName assemblylName, string? name = null, bool isCollectible = true)
        { 
            this.FilePath = String.Empty;
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
            var filePath = Path.GetDirectoryName(this.FilePath);
            var resolvedPath = (new AssemblyDependencyResolver(filePath)).ResolveAssemblyToPath(name);
            if (!String.IsNullOrEmpty(resolvedPath) && File.Exists(resolvedPath))
            {
                return context.LoadFromAssemblyPath(resolvedPath ?? String.Empty);
            }

            var manualPath = Path.Combine(filePath, name.Name + ".dll");
            if (File.Exists(manualPath))
            {
                return context.LoadFromAssemblyPath(manualPath ?? String.Empty);
            }

            return default;
        }

        private Assembly? loadAssembly()
        {
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
                    this.FilePath = assembly.Location;
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

            var instanceType = this.loadContext.Assemblies.SelectMany(o => o.GetTypes()).FirstOrDefault(t => t.FullName?.EndsWith(className) == true);

            return (instanceType == null) ? null : Activator.CreateInstance(instanceType);
        }

        public static AssemblyContext LoadFromPath(string filePath, string? name = null, bool isCollectible = true)
        {
            return new AssemblyContext(filePath, name, isCollectible);
        }

        /// <summary>
        /// Attempts to create an instance from the current assembly given a class name.
        /// If the class does not exist in this assembly a null object is returned.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public T? GetInstance<T>(string className)
        {
            if (!className.Contains('.')) className = '.' + className;
            var instanceType = this.loadAssembly()?.GetTypes()?.FirstOrDefault(o => o.FullName?.EndsWith(className) == true);
            return (instanceType == null) ? default : (T?)Activator.CreateInstance(instanceType);
        }
        /// <summary>
        /// restrict file path and translat to fully qualified file name
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="restrictedPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static string VerifyPath(string filePath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            var fullFilePath = Path.GetFullPath(filePath);

            return fullFilePath;
        }
        
        /// <summary>
        /// attempt to unload the load context
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            if (!this.loadContext.IsCollectible || !this.isLoaded) return false;
            
            try
            {
                this.loadContext.Unload();
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
}
