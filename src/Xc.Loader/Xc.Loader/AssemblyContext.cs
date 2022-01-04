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
    /// class for managing dynamic assembly loading
    /// </summary>
    public class AssemblyContext : IAssemblyContext, IDisposable
    {
        private bool disposed;

        /// <summary>
        /// full assembly file path
        /// </summary>
        public string FilePath { get; private set; }
        /// <summary>
        /// load context for managing unloading
        /// </summary>
        public AssemblyLoadContext LoadContext { get; }

        /// <summary>
        /// loaded assembly
        /// </summary>
        public System.Reflection.Assembly Assembly { get; private set; }
        /// <summary>
        /// assembly name refrence
        /// </summary>
        public AssemblyName Name { get; }
        /// <summary>
        /// list of types in assembly
        /// </summary>
        public IReadOnlyList<Type> Types { get; private set; } = new List<Type>();
        /// <summary>
        /// list of namespaces in assembly
        /// </summary>
        public IReadOnlyList<string> NameSpaces { get; private set; } = new List<string>();
        
        /// <summary>
        /// create an instance of an assembly from a path
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isCollectible"></param>
        public AssemblyContext(string filePath, bool isCollectible = true)
        {
            this.FilePath = VerifyPath(filePath);

            this.LoadContext = new System.Runtime.Loader.AssemblyLoadContext(null, isCollectible);
            this.Assembly = this.LoadContext.LoadFromAssemblyPath(this.FilePath);

            this.Name = this.Assembly.GetName();

            this.getAssemblyInfo();
        }
        /// <summary>
        /// create an instance of an assembly by name
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isCollectible"></param>
        public AssemblyContext(AssemblyName name, bool isCollectible = true)
        {
            this.Name = name;

            this.LoadContext = new System.Runtime.Loader.AssemblyLoadContext(null, isCollectible);
            this.Assembly = this.LoadContext.LoadFromAssemblyName(name);

            this.FilePath = this.Assembly.Location;

            this.getAssemblyInfo();
        }
        /// <summary>
        /// Attempts to create an instance from the current assembly given a class name.
        /// If the class does not exist in this assembly a null object is returned.
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public object? GetInstance(string className)
        {
            if (!className.Contains('.')) className = '.' + className;
            var instanceType = this.Types.FirstOrDefault(o => o.FullName?.EndsWith(className) == true);
            return instanceType == null ? null : this.Assembly.CreateInstance(instanceType.FullName);
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
        /// collect types and namespaces
        /// </summary>
        /// <returns></returns>
        private IReadOnlyList<Type> getAssemblyInfo()
        {
            if (this.Types.Count == 0) this.Types = this.Assembly.GetTypes().Where(o => o.IsPublic).ToList();
            if (this.NameSpaces.Count == 0) this.NameSpaces = this.Types.Where(o => !String.IsNullOrEmpty(o.Namespace)).Select(t => (string)(t.Namespace ?? String.Empty))?.Distinct().ToList() ?? new List<string>();
            return this.Types;
        }
        /// <summary>
        /// attempt to unload the load context
        /// </summary>
        /// <returns></returns>
        public bool Unload()
        {
            try
            {
                this.Assembly = default;
                this.Types = new List<Type>();
                this.LoadContext.Unload();
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
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

            this.disposed = true;

            this.Unload();
        }
    }
}
