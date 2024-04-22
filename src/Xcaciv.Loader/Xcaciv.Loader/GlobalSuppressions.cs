// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.LoadFromPath(System.String,System.String,System.Boolean)~Xcaciv.Loader.IAssemblyContext")]
[assembly: SuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.GetInstance(System.String)~System.Object")]
[assembly: SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.GetInstance(System.String)~System.Object")]
[assembly: SuppressMessage("Trimming", "IL2046:'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.GetInstance``1(System.String)~``0")]
[assembly: SuppressMessage("Trimming", "IL2046:'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.GetAllInstances``1~System.Collections.Generic.IEnumerable{``0}")]
[assembly: SuppressMessage("Trimming", "IL2046:'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.", Justification = "Purpose of class is to load unrefrenced code.", Scope = "member", Target = "~M:Xcaciv.Loader.AssemblyContext.GetVersion~System.Version")]
