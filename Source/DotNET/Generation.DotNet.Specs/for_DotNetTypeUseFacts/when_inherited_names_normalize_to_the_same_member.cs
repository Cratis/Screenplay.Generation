// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_inherited_names_normalize_to_the_same_member : given.a_compilation
{
    IReadOnlyList<GenerationFact> _facts;
    AdapterContributionAdmissionResult _admission;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/workspace/Collision.cs", "public class Base { public string URL { get; } } public class Derived : Base { public string uRL { get; } }"));
        var project = new DotNetProjectCompilation { Name = "Banking", Compilation = compilation, AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet() };
        var adapter = new AdapterIdentity { Id = "surface", Version = "1.0.0" };
        var type = TypeNamed(compilation, "Derived");
        _facts = DotNetTypeUseFacts.EmitWithSurface(
            type,
            new ArtifactKey { Subject = project.SubjectForType(type), Kind = ArtifactKind.Event },
            new DotNetAnalysisContext([project]),
            new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact },
            DotNetMemberSurfaceScope.Inherited);
        var descriptor = new AdapterDescriptor
        {
            Identity = adapter,
            SourceLanguage = AdapterSourceLanguage.CSharp,
            Category = AdapterCategory.ApplicationFramework,
            EmittedFactCapabilities = [GenerationFactCapability.ArtifactMemberDeclaration, GenerationFactCapability.ArtifactMemberTypeUse]
        };
        _admission = AdapterContributionAdmission.Admit(descriptor, new AdapterContribution { Adapter = adapter, Facts = _facts });
    }

    [Fact] void should_preserve_both_assertions_for_conflict_handling() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name).ShouldEqual("uRL", "uRL");
    [Fact] void should_use_distinct_raw_name_ids() => _facts.Select(fact => fact.Id).Distinct().Count().ShouldEqual(4);
    [Fact] void should_admit_the_independent_assertions() => _admission.IsAdmitted.ShouldBeTrue();
}
