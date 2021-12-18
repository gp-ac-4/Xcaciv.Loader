using System.Reflection;

namespace Xc.Loader
{
    /// <summary>
    /// class for managing dynamic assembly loading
    /// </summary>
    public interface IAssemblyContext
    {
        Assembly Assembly { get; }
        string FilePath { get; }
        List<string> NameSpaces { get; }
        List<Type> Types { get; }
    }
}