using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xc.Loader
{
    /// <summary>
    /// class for managing dynamic assembly loading
    /// </summary>
    public class AssemblyContext : IAssemblyContext
    {
        /// <summary>
        /// full assembly file path
        /// </summary>
        public string FilePath { get; private set; }
        /// <summary>
        /// loaded assembly
        /// </summary>
        public System.Reflection.Assembly Assembly { get; private set; }
        /// <summary>
        /// list of types in assembly
        /// </summary>
        public List<Type> Types { get; private set; } = new List<Type>();
        /// <summary>
        /// list of namespaces in assembly
        /// </summary>
        public List<string> NameSpaces { get; private set; } = new List<string>();
        /// <summary>
        /// create an instance of a loaded assembly
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="restrictedPath"></param>
        public AssemblyContext(string filePath, string? restrictedPath = null)
        {
            this.FilePath = verifyPath(filePath, restrictedPath);

            this.Assembly = System.Reflection.Assembly.LoadFrom(this.FilePath);

            this.getAssemblyInfo();
        }
        /// <summary>
        /// restrict file path and translat to fully qualified file name
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="restrictedPath"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static string verifyPath(string filePath, string? restrictedPath)
        {
            if (filePath == null) throw new ArgumentNullException(nameof(filePath));
            var fullFilePath = Path.GetFullPath(filePath);

            if (restrictedPath == null) restrictedPath = Directory.GetCurrentDirectory();
            var fullRestrictedPath = Path.GetFullPath(restrictedPath);

            if ((new Uri(fullRestrictedPath)).IsBaseOf(new Uri(fullFilePath)))
                throw new ArgumentOutOfRangeException(nameof(filePath) + " must be located within " + nameof(restrictedPath));

            return fullFilePath;
        }
        /// <summary>
        /// collect types and namespaces
        /// </summary>
        /// <returns></returns>
        private List<Type> getAssemblyInfo()
        {
            if (this.Types.Count == 0) this.Types = Assembly.GetTypes().ToList();
            if (this.NameSpaces.Count == 0) this.NameSpaces = this.Types.Where(o => !String.IsNullOrEmpty(o.Namespace)).Select(t => (string)(t.Namespace ?? String.Empty))?.Distinct().ToList() ?? new List<string>();
            return this.Types;
        }

    }
}
