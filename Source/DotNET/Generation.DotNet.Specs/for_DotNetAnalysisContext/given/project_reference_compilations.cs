// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAnalysisContext.given;

public class project_reference_compilations : DotNet.given.a_compilation
{
    protected const string ReferencedTypeMetadataName = "Referenced.Contracts.ReferencedType";

    protected static ProjectReferenceContext ProjectReferenceAt(string physicalRoot)
    {
        var ownerCompilation = CompilationFrom(new SourceFile(
                $"{physicalRoot}/Referenced/ReferencedType.cs",
                "namespace Referenced.Contracts; public record ReferencedType;"))
            .WithAssemblyName("Referenced.Project");
        var projectReference = ownerCompilation.ToMetadataReference();
        var rootCompilation = CompilationFrom(new SourceFile(
                $"{physicalRoot}/Root/UsesReferencedType.cs",
                "namespace Root; public record UsesReferencedType(Referenced.Contracts.ReferencedType Value);"))
            .WithAssemblyName("Root.Project")
            .AddReferences(projectReference);
        var owner = Project("Referenced", ownerCompilation, $"{physicalRoot}/Referenced/Referenced.csproj");
        var root = Project("Root", rootCompilation, $"{physicalRoot}/Root/Root.csproj");

        return new ProjectReferenceContext(
            owner,
            root,
            TypeNamed(ownerCompilation, ReferencedTypeMetadataName),
            TypeNamed(rootCompilation, ReferencedTypeMetadataName),
            projectReference);
    }

    protected static CompetingAssemblyContext ProjectReferenceWithCompetingAssemblyAt(string physicalRoot)
    {
        var ownerCompilation = CompilationFrom(new SourceFile(
                $"{physicalRoot}/Owner/ReferencedType.cs",
                "[assembly: System.Reflection.AssemblyVersion(\"1.2.3.4\")] namespace Referenced.Contracts; public record ReferencedType;"))
            .WithAssemblyName("Shared.Assembly");
        var projectReference = ownerCompilation.ToMetadataReference();
        var rootCompilation = CompilationFrom(new SourceFile(
                $"{physicalRoot}/Root/UsesReferencedType.cs",
                "namespace Root; public record UsesReferencedType(Referenced.Contracts.ReferencedType Value);"))
            .WithAssemblyName("Root.Project")
            .AddReferences(projectReference);
        var competingCompilation = CompilationFrom(new SourceFile(
                $"{physicalRoot}/Competing/ReferencedType.cs",
                "[assembly: System.Reflection.AssemblyVersion(\"9.8.7.6\")] namespace Referenced.Contracts; public record ReferencedType;"))
            .WithAssemblyName("Shared.Assembly");
        var owner = Project("Owner", ownerCompilation, $"{physicalRoot}/Owner/Owner.csproj");
        var root = Project("Root", rootCompilation, $"{physicalRoot}/Root/Root.csproj");
        var competing = Project("Competing", competingCompilation, $"{physicalRoot}/Competing/Competing.csproj");

        return new CompetingAssemblyContext(
            owner,
            root,
            competing,
            TypeNamed(ownerCompilation, ReferencedTypeMetadataName),
            TypeNamed(rootCompilation, ReferencedTypeMetadataName),
            projectReference);
    }

    protected static DotNetProjectCompilation Project(string name, CSharpCompilation compilation, string projectPath) =>
        new()
        {
            Name = name,
            ProjectPath = projectPath,
            SourceRoot = Path.GetDirectoryName(projectPath),
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };

    protected sealed record ProjectReferenceContext(
        DotNetProjectCompilation Owner,
        DotNetProjectCompilation Root,
        INamedTypeSymbol OwnerType,
        INamedTypeSymbol ReferencedType,
        MetadataReference ProjectReference);

    protected sealed record CompetingAssemblyContext(
        DotNetProjectCompilation Owner,
        DotNetProjectCompilation Root,
        DotNetProjectCompilation Competing,
        INamedTypeSymbol OwnerType,
        INamedTypeSymbol ReferencedType,
        MetadataReference ProjectReference);
}
