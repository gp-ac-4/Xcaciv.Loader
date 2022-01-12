# Xc.Loader

Sexy simple C# module for runtime loading of types from exteranl assemblies.

```csharp
    using (var context = AssemblyContext.LoadFromPath(dllPath)) // Load
    {
        var myInstance = context.GetInstance<IClass1>("Class1");
        return myInstance.Stuff("input here");
    } // Unload
```