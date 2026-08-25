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
}
