using System.Reflection;

namespace Xc.Loader
{
    /// <summary>
    /// class for managing dynamic assembly loading
    /// </summary>
    public interface IAssemblyContext: IDisposable
    {
        /// <summary>
        /// Assembly instance for this context
        /// </summary>
        Assembly Assembly { get; }
        string FilePath { get; }
        AssemblyName Name { get; }
        IReadOnlyList<string> NameSpaces { get; }
        IReadOnlyList<Type> Types { get; }

        bool Unload();
    }
}