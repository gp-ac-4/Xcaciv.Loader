# Xcaciv.Loader

Sexy simple C# module for runtime loading of types from external assemblies.

```csharp
    using (var context = AssemblyContext.LoadFromPath(dllPath)) // Load
    {
        var myInstance = context.GetInstance<IClass1>("Class1");
        return myInstance.Stuff("input here");
    } // Unload
```

## Specification

For detailed specifications, see [the specification document](spec/spec-architecture-dynamic-assembly-loading.md).