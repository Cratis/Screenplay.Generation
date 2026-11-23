// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_projecting_exact_type_uses : given.compatibility_type_uses
{
    GeneratedScreenplayDefinition _result = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    GeneratedScreenplayDefinition _strict = null!;
    GeneratedScreenplayDefinition _bound = null!;
    GeneratedScreenplayDefinition _fallback = null!;
    GeneratedScreenplayDefinition _nonConcept = null!;
    GeneratedScreenplayDefinition _explicit = null!;

    void Because()
    {
        var facts = ProjectedCommand();
        _result = Generate([.. facts, .. Candidate("enum", false)]);
        _reverse = Generate([.. facts.Reverse(), .. Candidate("enum", false).Reverse()]);
        _strict = Generate(facts.Where(fact => fact is not TypeUseCompatibilityProjectionFact));
        var concept = Concept("Container", GenerationPrimitiveKind.Text);
        var selected = ProjectedCommand(concept.Subject, "Container", true);
        _bound = Generate([.. selected, .. concept.Facts]);
        _fallback = Generate(selected);
        var nonConcept = NonConcept() with { Subject = concept.Subject, Definition = NonConcept().Definition with { Artifact = new() { Subject = concept.Subject, Kind = ArtifactKind.ReadModel }, Name = "ContainerRecord" } };
        _nonConcept = Generate([.. selected, nonConcept]);
        var observation = selected.OfType<ArtifactMemberTypeUseFact>().Single();
        _explicit = Generate([.. selected, nonConcept, new TypeUseBindingFact
        {
            Id = Id("explicit-container"), Subject = observation.Subject, Evidence = observation.Evidence,
            Definition = new() { Member = observation.Definition.Member, Target = nonConcept.Definition.Artifact }
        }]);
    }

    [Fact] void should_lower_the_explicit_view() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_print_the_intermediate_name() => _result.Source.ShouldContain("permissions List[]");
    [Fact] void should_keep_the_exact_original_shape() => _result.AdapterRun!.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.Shape.Count.ShouldEqual(3);
    [Fact] void should_keep_the_hidden_terminal_subject() => _result.AdapterRun!.Facts.Select(record => record.Fact).OfType<ArtifactMemberTypeUseFact>().Single().Definition.Type.ObservedTypeSubject.ShouldEqual(Target);
    [Fact] void should_not_bind_the_hidden_terminal_concept() => _result.AdapterRun!.Derivation.Facts.Any(record => record.Fact is TypeUseBindingFact).ShouldBeFalse();
    [Fact] void should_keep_the_projection_as_provenance() => _result.AdapterRun!.Facts.Single(record => record.Fact is TypeUseCompatibilityProjectionFact).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_not_project_without_opt_in() => _strict.IsSuccess.ShouldBeFalse();
    [Fact] void should_bind_the_selected_concept_identity() => Member(_bound).Type.Subject.ShouldEqual(Concept("Container", GenerationPrimitiveKind.Text).Subject);
    [Fact] void should_keep_plain_projected_fallback() => _fallback.Source.ShouldContain("permissions Container[]");
    [Fact] void should_not_bind_a_selected_non_concept() => Member(_nonConcept).Type.Subject.ShouldBeNull();
    [Fact] void should_reject_explicit_non_concept_bindings() => _explicit.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingArtifactMember).ShouldBeTrue();
    [Fact] void should_include_the_projection_in_binding_lineage() => _bound.AdapterRun!.Derivation.Facts.Single(record => record.Fact is TypeUseBindingFact).Lineage.Inputs.ShouldContain(ProjectedCommand().OfType<TypeUseCompatibilityProjectionFact>().Single().Id);
    [Fact] void should_keep_order_independent_source() => _reverse.Source.ShouldEqual(_result.Source);
    [Fact] void should_keep_order_independent_diagnostics() => Projection(_reverse.Diagnostics).ShouldEqual(Projection(_result.Diagnostics));
    [Fact] void should_keep_order_independent_lineage() => Projection(_reverse.AdapterRun!.Derivation).ShouldEqual(Projection(_result.AdapterRun!.Derivation));
    [Fact] void should_replay_from_its_snapshot() => Generator.Generate(_result.AdapterRun!, new() { Domain = "Ordering" }).Source.ShouldEqual(_result.Source);
}
