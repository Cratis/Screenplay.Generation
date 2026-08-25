// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.Vogen.for_VogenConceptScreenplayAdapter;

public class when_nominating_a_registered_concept : given.a_vogen_compilation
{
    IReadOnlyList<GenerationFact> _facts = null!;
    ArtifactFact _concept = null!;
    ConceptRepresentationFact _representation = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            "Ordering",
            new SourceFile(
                "/workspace/Concepts/OrderNumber.cs",
                "namespace Ordering.Concepts; public readonly record struct OrderNumber(string Value);"));
        var project = Project("Ordering", compilation);
        var wrapper = compilation.GetTypeByMetadataName("Ordering.Concepts.OrderNumber");
        var subject = project.SubjectForType(wrapper);
        var evidence = DotNetSource.EvidenceFor(
            wrapper,
            new AdapterIdentity { Id = "registered-values", Version = "1.0.0" },
            project,
            EvidenceStrength.Configured,
            "An authored registration declares the source type as a domain value");

        _facts = DotNetConceptFacts.Emit(
            wrapper,
            compilation.GetSpecialType(SpecialType.System_String),
            subject,
            evidence);
        _concept = _facts.OfType<ArtifactFact>().Single();
        _representation = _facts.OfType<ConceptRepresentationFact>().Single();
    }

    [Fact] void should_emit_the_concept_before_its_representation() => _facts.Select(_ => _.GetType()).ShouldEqual([typeof(ArtifactFact), typeof(ConceptRepresentationFact)]);
    [Fact] void should_emit_a_concept_artifact() => _concept.Definition.Key.Kind.ShouldEqual(ArtifactKind.Concept);
    [Fact] void should_keep_the_wrapper_name() => _concept.Definition.Name.ShouldEqual("OrderNumber");
    [Fact] void should_keep_the_declaration_file() => _concept.Definition.File.ShouldEqual("Concepts/OrderNumber.cs");
    [Fact] void should_emit_the_primitive_representation() => _representation.Definition.Primitive.ShouldEqual(GenerationPrimitiveKind.Text);
    [Fact] void should_keep_the_exact_subject() => _representation.Definition.Concept.ShouldEqual(_concept.Subject);
    [Fact] void should_keep_the_registration_evidence() => _facts.All(_ => _.Evidence.Strength == EvidenceStrength.Configured).ShouldBeTrue();
    [Fact]
    void should_use_stable_producer_owned_fact_ids() => _facts.Select(_ => _.Id.Value).ShouldEqual(
        [
            $"registered-values:concept:{_concept.Subject.Value}",
            $"registered-values:concept-representation:{_concept.Subject.Value}"
        ]);
}
