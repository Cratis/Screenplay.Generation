// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeUseFacts;

public class when_emitting_member_facts : given.a_compilation
{
    readonly AdapterIdentity _adapter = new() { Id = "critter-stack", Version = "1.0.0" };
    ArtifactKey _artifact = null!;
    IReadOnlyList<GenerationFact> _facts = null!;
    SubjectId _conceptSubject = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Ordering/RegisterCustomer.cs",
            """
            #nullable enable
            namespace Ordering;
            public sealed record CustomerCode;
            public sealed record RegisterCustomer(CustomerCode CustomerCode, string? Referral);
            """));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        _artifact = new ArtifactKey
        {
            Subject = project.SubjectForType(TypeNamed(compilation, "Ordering.RegisterCustomer")),
            Kind = ArtifactKind.Command
        };
        _conceptSubject = project.SubjectForType(TypeNamed(compilation, "Ordering.CustomerCode"));
        _facts = DotNetTypeUseFacts.Emit(
            TypeNamed(compilation, "Ordering.RegisterCustomer"),
            _artifact,
            context,
            new Evidence { Adapter = _adapter, Strength = EvidenceStrength.Exact },
            property => property.Name == "CustomerCode"
                ? ArtifactMemberRoleKind.EventSourceIdentifier
                : null);
    }

    [Fact] void should_emit_one_declaration_per_member() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.Member.Name).ShouldEqual("customerCode", "referral");
    [Fact] void should_preserve_zero_based_declaration_order() => _facts.OfType<ArtifactMemberDeclarationFact>().Select(fact => fact.Definition.DeclarationOrder).ShouldEqual(0, 1);
    [Fact] void should_emit_one_exact_type_use_per_member() => _facts.OfType<ArtifactMemberTypeUseFact>().Count().ShouldEqual(2);
    [Fact] void should_bind_the_terminal_source_subject_without_inspecting_other_adapters() => _facts.OfType<ArtifactMemberTypeUseFact>().First().Definition.Type.ObservedTypeSubject.ShouldEqual(_conceptSubject);
    [Fact] void should_preserve_optional_reference_shape() => string.Join('|', _facts.OfType<ArtifactMemberTypeUseFact>().Last().Definition.Type.Shape).ShouldEqual("Optional|Named");
    [Fact] void should_emit_only_the_explicitly_established_role() => _facts.OfType<ArtifactMemberRoleFact>().Single().Definition.ShouldEqual(new ArtifactMemberRoleDefinition { Member = new ArtifactMemberKey { Artifact = _artifact, Name = "customerCode" }, Role = ArtifactMemberRoleKind.EventSourceIdentifier });
    [Fact] void should_scope_every_fact_id_to_the_evidence_adapter() => _facts.All(fact => fact.Id.Value.StartsWith("critter-stack:", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_retain_the_exact_artifact_owner_on_every_fact() => _facts.All(fact => fact.Subject == _artifact.Subject).ShouldBeTrue();
}
