// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.PackageBuilder.#NuGet.IPackageMetadata.FrameworkAssemblies")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "NuGet.Analysis.Rules", Justification = "Don't want to pollute the NuGet namespace.")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1703:ResourceStringsShouldBeSpelledCorrectly", MessageId = "ps", Scope = "resource", Target = "NuGet.Resources.AnalysisResources.resources")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "NuGet.ManifestMetadata.#NuGet.IPackageMetadata.PackageAssemblyReferences")]

// Suppressions for NuGet.Frameworks code.
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors", Scope = "type", Target = "NuGet.Frameworks.FrameworkException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic", Scope = "type", Target = "NuGet.Frameworks.FrameworkException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1307:SpecifyStringComparison", MessageId = "System.String.CompareTo(System.String)", Scope = "member", Target = "NuGet.Frameworks.FrameworkRuntimePair.#CompareTo(NuGet.Frameworks.FrameworkRuntimePair)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "NuGet.Frameworks.NuGetFramework.#GetShortFolderName(NuGet.Frameworks.IFrameworkNameProvider)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Scope = "member", Target = "NuGet.Frameworks.NuGetFramework.#TryParseCommonFramework(System.String,NuGet.Frameworks.NuGetFramework&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "NuGet.Frameworks.FrameworkNameProvider.#AddCompatibleCandidates()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "NuGet.Frameworks.FrameworkReducer.#GetNearestInternal(NuGet.Frameworks.NuGetFramework,System.Collections.Generic.IEnumerable`1<NuGet.Frameworks.NuGetFramework>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "NuGet.Frameworks.FrameworkReducer.#IsBetterPCL(NuGet.Frameworks.NuGetFramework,NuGet.Frameworks.NuGetFramework)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Scope = "member", Target = "NuGet.Frameworks.NuGetFramework.#TryParseCommonFramework(System.String,NuGet.Frameworks.NuGetFramework&)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "NuGet.Frameworks.CompatibilityListProvider.#ReduceDownwards(System.Collections.Generic.IEnumerable`1<NuGet.Frameworks.NuGetFramework>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "NuGet.Frameworks.CompatibilityProvider.#IsSpecialFrameworkCompatible(NuGet.Frameworks.NuGetFramework,NuGet.Frameworks.NuGetFramework)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "NuGet.Frameworks.DefaultPortableFrameworkMappings.#CreateProfileFrameworks(System.Int32,NuGet.Frameworks.NuGetFramework[])")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Scope = "member", Target = "NuGet.Frameworks.FrameworkNameProvider.#AddFrameworkPrecedenceMappings(System.Collections.Generic.IDictionary`2<System.String,System.Int32>,System.Collections.Generic.IEnumerable`1<System.String>)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "includeOptional", Scope = "member", Target = "NuGet.Frameworks.FrameworkReducer.#ExplodePortableFrameworks(System.Collections.Generic.IEnumerable`1<NuGet.Frameworks.NuGetFramework>,System.Boolean)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "mappings", Scope = "member", Target = "NuGet.Frameworks.CompatibilityTable.#GetTable(System.Collections.Generic.IEnumerable`1<NuGet.Frameworks.NuGetFramework>,NuGet.Frameworks.IFrameworkNameProvider,NuGet.Frameworks.IFrameworkCompatibilityProvider)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Scope = "type", Target = "NuGet.Frameworks.FrameworkException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sem", Scope = "member", Target = "NuGet.SemanticVersion.#IsSemVer2()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "SemVer", Scope = "member", Target = "NuGet.SemanticVersion.#IsSemVer2()")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Ver", Scope = "member", Target = "NuGet.SemanticVersion.#IsSemVer2()")]

