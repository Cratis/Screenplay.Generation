// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_inherited_properties_lack_authored_authority : given.a_compilation
{
    IReadOnlyList<GenerationFact> _facts;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile("/workspace/Generated.g.cs", "public class Generated : System.Exception { public string GeneratedProperty { get; } }"),
            new SourceFile("/workspace/Ordinary.cs", "public class NonAuthored : Generated { public string NonAuthoredProperty { get; } }"),
            new SourceFile("/workspace/Authored.cs", "public class Authored : NonAuthored { public string Local { get; } }"));
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.Where(tree => tree.FilePath != "/workspace/Ordinary.cs").ToHashSet()
        };
        _facts = DotNetTypeUseFacts.EmitWithSurface(
            TypeNamed(compilation, "Authored"),
            new ArtifactKey { Subject = new SubjectId { Value = "authored" }, Kind = ArtifactKind.Event },
            new DotNetAnalysisContext([project]),
            new Evidence { Adapter = new AdapterIdentity { Id = "surface", Version = "1.0.0" }, Strength = EvidenceStrength.Exact },
            DotNetMemberSurfaceScope.Inherited);
    }

    [Fact] void should_exclude_generated_non_authored_and_metadata_base_members() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name).ShouldEqual("local");
}
