---
title: Dynamic Assembly Loading with Isolation and Unloading
version: 1.0
date_created: 2025-10-12
owner: Xcaciv
tags: [architecture, design, library]
---

## Introduction

This specification defines the requirements and architecture for a .NET library that enables dynamic assembly loading with isolation and controlled unloading capabilities. The library provides a secure mechanism to load external assemblies at runtime while maintaining separation from the host application's assembly context.

## 1. Purpose & Scope

The purpose of this library is to provide a simple and secure mechanism for dynamically loading .NET assemblies at runtime with proper isolation and unloading capabilities. This is particularly useful for plugin architectures, modular applications, and scenarios where components need to be loaded/unloaded dynamically without affecting the host application.

Target audience:

- Application developers building extensible applications
- Plugin framework developers
- Libraries requiring dynamic type loading

Scope:

- Runtime assembly loading with path verification
- Type discovery and instantiation
- Assembly isolation through custom load contexts
- Assembly unloading support
- Support for dependency resolution

## 2. Definitions

- **Assembly**: A compiled code library in .NET, typically a DLL file.
- **Assembly Load Context**: A .NET mechanism that defines an isolation boundary for loaded assemblies.
- **Dynamic Loading**: Loading assemblies at runtime rather than compile-time.
- **Collectible Assembly Load Context**: A load context that can be unloaded from memory when no longer needed.
- **Type Activation**: The process of creating an instance of a type from a loaded assembly.
- **Path Verification**: Ensuring that assembly paths meet security and access requirements before loading.
- **Dependency Resolution**: The process of finding and loading dependent assemblies.
- **GAC**: Global Assembly Cache, a machine-wide cache for .NET assemblies.

## 3. Requirements, Constraints & Guidelines

### Core Requirements

- **REQ-001**: Provide isolated loading of assemblies at runtime.
- **REQ-002**: Support unloading of dynamically loaded assemblies.
- **REQ-003**: Offer type discovery from loaded assemblies.
- **REQ-004**: Enable instantiation of types from loaded assemblies.
- **REQ-005**: Support automatic dependency resolution for loaded assemblies.
- **REQ-006**: Implement path verification for security.

### Security Requirements

- **SEC-001**: Implement path restrictions for assembly loading to prevent arbitrary code execution.
- **SEC-002**: Validate assembly paths to ensure they exist within allowed locations.
- **SEC-003**: Isolate loaded assemblies to prevent interference with the host application.

### Performance Requirements

- **PER-001**: Minimize memory leaks by properly unloading assemblies when requested.
- **PER-002**: Support efficient type discovery and creation.

### Constraints

- **CON-001**: Compatible with .NET 8.0 or later.
- **CON-002**: No external dependencies beyond the .NET runtime.
- **CON-003**: Must work with AOT and trimmed applications with appropriate suppression attributes.

### Guidelines

- **GUD-001**: Implement IDisposable pattern for proper resource cleanup.
- **GUD-002**: Use strong typing where possible while maintaining flexibility.
- **GUD-003**: Provide clear documentation for security implications.
- **GUD-004**: Handle exceptions appropriately without swallowing critical errors.

## 4. Interfaces & Data Contracts

### IAssemblyContext Interface

The primary interface for interacting with loaded assemblies:

```csharp
public interface IAssemblyContext : IDisposable
{
    string FilePath { get; }
    string FullAssemblyName { get; }
    object? CreateInstance(string className);
    T CreateInstance<T>(string className);
    T CreateInstance<T>(Type classType);
    IEnumerable<Type>? GetTypes();
    IEnumerable<Type>? GetTypes(Type baseType);
    IEnumerable<Type> GetTypes<T>();
    Version GetVersion();
    bool Unload();
}
```

### AssemblyContext Implementation

Main implementation class with the following key behaviors:

- Constructor accepts file path, assembly name, collectible flag, and base path restriction
- Path verification ensures assemblies are loaded only from approved locations
- Support for both path-based and name-based assembly loading
- Automatic dependency resolution for loaded assemblies
- Clean unloading of assemblies when requested

## 5. Acceptance Criteria

- **AC-001**: Given a valid assembly path, when the AssemblyContext is created, then the assembly is loaded successfully.
- **AC-002**: Given a loaded assembly, when GetTypes() is called, then all types from the assembly are returned.
- **AC-003**: Given a loaded assembly with a specific type, when CreateInstance() is called with that type name, then an instance is created successfully.
- **AC-004**: Given a loaded assembly, when Unload() is called, then the assembly is unloaded from memory.
- **AC-005**: Given an assembly path outside the base path restriction, when creating an AssemblyContext, then an ArgumentOutOfRangeException is thrown.
- **AC-006**: Given a dependent assembly, when the main assembly is loaded, then its dependencies are resolved automatically.
- **AC-007**: Given a collectible AssemblyContext, when disposed, then the assembly is unloaded properly.

## 6. Test Automation Strategy

- **Test Levels**:
  - Unit tests for AssemblyContext class
  - Integration tests with test assemblies

- **Frameworks**:
  - XUnit for test assertions
  - MSTest for test organization

- **Test Data Management**:
  - Test assemblies with known types
  - Test assemblies with dependencies
  - Invalid paths for negative testing

- **Coverage Requirements**:
  - Path verification
  - Assembly loading
  - Type discovery and creation
  - Dependency resolution
  - Assembly unloading
  - Error handling

## 7. Rationale & Context

Dynamic assembly loading is essential for extensible applications and plugin architectures. However, the standard .NET assembly loading mechanism does not provide proper isolation or unloading capabilities, which can lead to memory leaks and application instability.

This library addresses these issues by:

1. Using AssemblyLoadContext with collectible flag for proper isolation and unloading
2. Implementing path verification for security
3. Providing automatic dependency resolution
4. Wrapping complex assembly loading operations in a simple interface

The design choices prioritize:

- Safety (path verification, isolation)
- Reliability (proper unloading, dependency resolution)
- Simplicity (easy-to-use interface)
- Performance (minimal memory overhead)

## 8. Dependencies & External Integrations

### Technology Platform Dependencies

- **PLT-001**: .NET Runtime 8.0 or later
- **PLT-002**: System.Runtime.Loader.AssemblyLoadContext API

### Infrastructure Dependencies

- **INF-001**: File system access for loading assemblies

## 9. Examples & Edge Cases

### Basic Usage Example

```csharp
// Load an assembly and create an instance
using (var context = new AssemblyContext("path/to/assembly.dll", basePathRestriction: "path/to"))
{
    // Create instance by class name
    var instance = context.CreateInstance<IMyInterface>("MyClass");
    
    // Use the instance
    instance.DoSomething();
    
    // Assembly is automatically unloaded when context is disposed
}
```

### Edge Cases

- **Loading assemblies with circular dependencies**: The library should handle this gracefully.
- **Unloading assemblies that have static references**: May not fully unload until all references are cleared.
- **Loading assemblies with native dependencies**: These may require additional resolution steps.
- **Loading assemblies from network paths**: Should work but with performance implications.

## 10. Validation Criteria

- Successfully loads assemblies dynamically at runtime
- Properly isolates loaded assemblies from host application
- Correctly resolves assembly dependencies
- Successfully creates instances of types from loaded assemblies
- Unloads assemblies when requested
- Prevents loading from unauthorized paths
- Handles error conditions gracefully
- Works with AOT and trimmed applications

## 11. Related Specifications / Further Reading

- [.NET Assembly Loading Architecture](https://learn.microsoft.com/en-us/dotnet/standard/assembly/)
- [Understanding AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/understanding-assemblyloadcontext)
- [Collectible AssemblyLoadContext](https://learn.microsoft.com/en-us/dotnet/standard/assembly/unloadability)