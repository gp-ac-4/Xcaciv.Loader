# Xcaciv.Loader

Sexy simple C# module for runtime loading of types from external assemblies.

```csharp
    using (var context = AssemblyContext.LoadFromPath(dllPath)) // Load
    {
        var myInstance = context.GetInstance<IClass1>("Class1");
        return myInstance.Stuff("input here");
    } // Unload
```

## Features

- Dynamic assembly loading and unloading
- Type discovery and instantiation
- Security measures to prevent loading from restricted directories
- Automatic dependency resolution
- **Multi-framework support**: Build for both .NET 8.0 and .NET 10.0

## Multi-Framework Support

This library can be built for both .NET 8.0 (default) and .NET 10.0.

See the [Multi-Framework Documentation](docs/multi-framework.md) for details on how to build for different target frameworks.

## Specification

For detailed specifications, see [the specification document](spec/spec-architecture-dynamic-assembly-loading.md)