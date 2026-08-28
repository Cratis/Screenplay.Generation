// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_source_property_names_normalize_to_the_same_member : given.a_compilation
{
    AdapterContributionAdmissionResult _admission = null!;
    IReadOnlyList<GenerationFact> _facts = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Ordering/CaseCollision.cs",
            """
            namespace Ordering;
            public sealed record CaseCollision(string URL, string uRL);
            """));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        var adapter = new AdapterIdentity { Id = "case-collision", Version = "1.0.0" };
        _facts = DotNetTypeUseFacts.Emit(
            TypeNamed(compilation, "Ordering.CaseCollision"),
            new ArtifactKey
            {
                Subject = project.SubjectForType(TypeNamed(compilation, "Ordering.CaseCollision")),
                Kind = ArtifactKind.CompositeType
            },
            context,
            new Evidence { Adapter = adapter, Strength = EvidenceStrength.Exact });
        var descriptor = new AdapterDescriptor
        {
            Identity = adapter,
            SourceLanguage = AdapterSourceLanguage.CSharp,
            Category = AdapterCategory.ApplicationFramework,
            EmittedFactCapabilities =
            [
                GenerationFactCapability.ArtifactMemberDeclaration,
                GenerationFactCapability.ArtifactMemberTypeUse
            ]
        };
        _admission = AdapterContributionAdmission.Admit(
            descriptor,
            new AdapterContribution { Adapter = adapter, Facts = _facts });
    }

    [Fact] void should_retain_both_normalized_member_assertions() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name).ShouldEqual("uRL", "uRL");
    [Fact] void should_retain_distinct_declaration_orders_for_conflict_resolution() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.DeclarationOrder).ShouldEqual(0, 1);
    [Fact] void should_encode_the_exact_source_property_in_unique_fact_ids() => _facts.Select(fact => fact.Id.Value).Distinct(StringComparer.Ordinal).Count().ShouldEqual(4);
    [Fact] void should_admit_the_unique_source_assertions_atomically() => _admission.IsAdmitted.ShouldBeTrue();
}
