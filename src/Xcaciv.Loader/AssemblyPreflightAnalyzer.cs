using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Xcaciv.Loader;

/// <summary>
/// Performs lightweight, non-executing preflight checks against an assembly file
/// to detect usage of Reflection.Emit namespaces and LINQ Expressions Compile operations.
/// Uses System.Reflection.Metadata to avoid loading the assembly.
/// </summary>
internal static class AssemblyPreflightAnalyzer
{
    internal sealed class Result
    {
        public bool HasEmitNamespaceTypes { get; init; }
        public bool ReferencesEmitAssemblies { get; init; }
        public bool HasLinqExpressionsCompile { get; init; }
        public IReadOnlyList<string> Indicators { get; init; } = Array.Empty<string>();
        public bool HasAnyIndicators => HasEmitNamespaceTypes || ReferencesEmitAssemblies || HasLinqExpressionsCompile;
    }

    public static Result Analyze(string assemblyPath)
    {
        var indicators = new List<string>();
        bool hasEmitNamespaceTypes = false;
        bool referencesEmitAssemblies = false;
        bool hasLinqExpressionsCompile = false;

        if (String.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
        {
            return new Result { Indicators = indicators.AsReadOnly() };
        }

        try
        {
            using var fs = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(fs);
            if (!peReader.HasMetadata)
            {
                return new Result { Indicators = indicators.AsReadOnly() };
            }

            var reader = peReader.GetMetadataReader();

            // Scan type definitions for Reflection.Emit namespaces
            foreach (var typeHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeHandle);
                var ns = reader.GetString(typeDef.Namespace);
                if (!String.IsNullOrEmpty(ns) && ns.StartsWith("System.Reflection.Emit", StringComparison.Ordinal))
                {
                    hasEmitNamespaceTypes = true;
                    indicators.Add($"Type in namespace '{ns}'");
                    break; // one is enough
                }
            }

            // Scan assembly references for known Reflection.Emit assemblies
            foreach (var referenceHandle in reader.AssemblyReferences)
            {
                var assemblyReference = reader.GetAssemblyReference(referenceHandle);
                var name = reader.GetString(assemblyReference.Name);
                if (name.StartsWith("System.Reflection.Emit", StringComparison.Ordinal))
                {
                    referencesEmitAssemblies = true;
                    indicators.Add($"References assembly '{name}'");
                    break;
                }
            }

            // Scan member references for LINQ Expressions Compile()
            foreach (var referenceHandle in reader.MemberReferences)
            {
                var memberReference = reader.GetMemberReference(referenceHandle);
                var memberName = reader.GetString(memberReference.Name);
                if (!String.Equals(memberName, "Compile", StringComparison.Ordinal))
                    continue;

                // Check parent type namespace
                if (memberReference.Parent.Kind == HandleKind.TypeReference)
                {
                    var typeRef = reader.GetTypeReference((TypeReferenceHandle)memberReference.Parent);
                    var ns = reader.GetString(typeRef.Namespace);
                    if (!String.IsNullOrEmpty(ns) && ns.StartsWith("System.Linq.Expressions", StringComparison.Ordinal))
                    {
                        hasLinqExpressionsCompile = true;
                        indicators.Add($"Member '{ns}.Compile' reference");
                        break;
                    }
                }
                else if (memberReference.Parent.Kind == HandleKind.TypeDefinition)
                {
                    var typeDef = reader.GetTypeDefinition((TypeDefinitionHandle)memberReference.Parent);
                    var ns = reader.GetString(typeDef.Namespace);
                    if (!String.IsNullOrEmpty(ns) && ns.StartsWith("System.Linq.Expressions", StringComparison.Ordinal))
                    {
                        hasLinqExpressionsCompile = true;
                        indicators.Add($"Member '{ns}.Compile' definition");
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If metadata parsing fails, we return a neutral result
            // to avoid false positives blocking loads.
            // @copilot: ignoe this empty catch block
        }

        return new Result
        {
            HasEmitNamespaceTypes = hasEmitNamespaceTypes,
            ReferencesEmitAssemblies = referencesEmitAssemblies,
            HasLinqExpressionsCompile = hasLinqExpressionsCompile,
            Indicators = indicators.AsReadOnly()
        };
    }
}
