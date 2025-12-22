using System;
using System.Reflection;
using System.Reflection.Emit;
using zTestInterfaces;

namespace zTestRiskyAssembly;

/// <summary>
/// Class that uses Reflection.Emit to dynamically create types at runtime.
/// This assembly demonstrates risky behavior that should be detected by AssemblyPreflightAnalyzer.
/// </summary>
public class DynamicTypeCreator : IClass1
{
    public string Stuff(string input)
    {
        // Create a dynamic assembly and type using Reflection.Emit
        var assemblyName = new AssemblyName("DynamicAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule");
        
        var typeBuilder = moduleBuilder.DefineType("DynamicType", TypeAttributes.Public);
        var methodBuilder = typeBuilder.DefineMethod("GetMessage", MethodAttributes.Public, typeof(string), Type.EmptyTypes);
        var il = methodBuilder.GetILGenerator();
        il.Emit(OpCodes.Ldstr, "dynamic");
        il.Emit(OpCodes.Ret);
        
        var dynamicType = typeBuilder.CreateType();
        return input + " - created dynamic type: " + dynamicType.Name;
    }
}
