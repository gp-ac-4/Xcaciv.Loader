using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xc.Loader
{
    /// <summary>
    /// type utility for usage with IAssemblyContext
    /// </summary>
    public class TypeContext
    {
        /// <summary>
        /// refrence to assembly context
        /// </summary>
        private IAssemblyContext context;
        /// <summary>
        /// context for validating assembly types
        /// </summary>
        /// <param name="context"></param>
        public TypeContext(IAssemblyContext context)
        {
            this.context = context;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetInstance<T>(string typename)
        {
            var instanceType = this.context.Types.FirstOrDefault(T => T.FullName?.EndsWith(typename)??false);
            return (instanceType == null) ? default : (T)Activator.CreateInstance(instanceType);
        }

    
    }
}
