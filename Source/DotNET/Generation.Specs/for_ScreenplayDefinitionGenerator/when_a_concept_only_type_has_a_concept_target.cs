// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_concept_only_type_has_a_concept_target : given.concept_only_type_uses
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reverse;

    void Because()
    {
        var target = Candidate("enum", false, true);
        GenerationFact[] facts = [.. OptedCommand(TypeUseShapeKind.Optional, TypeUseShapeKind.Collection, TypeUseShapeKind.Named), .. target, NonConcept(), NonConcept(ArtifactKind.Event)];
        _result = Generate(facts);
        _reverse = Generate(facts.Reverse());
    }

    [Fact] void should_generate_compiler_verified_screenplay() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_bind_only_the_concept() => Member(_result).Type.TargetArtifactKind.ShouldEqual(ArtifactKind.Concept);
    [Fact] void should_keep_the_exact_subject() => Member(_result).Type.Subject.ShouldEqual(Target);
    [Fact] void should_keep_the_shape() => _result.Source.ShouldContain("permissions Permissions[] optional");
    [Fact] void should_keep_the_policy_in_binding_lineage() => _result.AdapterRun.Derivation.Facts.Single(record => record.Fact is TypeUseBindingFact).Lineage.Inputs.ShouldContain(Policy(Command().OfType<ArtifactMemberTypeUseFact>().Single()).Id);
    [Fact] void should_keep_the_policy_as_provenance() => _result.AdapterRun.Facts.Single(record => record.Fact is TypeUseBindingPolicyFact).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_have_order_independent_source() => _reverse.Source.ShouldEqual(_result.Source);
    [Fact] void should_have_order_independent_lineage() => Projection(_reverse.AdapterRun.Derivation).ShouldEqual(Projection(_result.AdapterRun.Derivation));
    [Fact] void should_not_report_missing_targets() => _result.Diagnostics.Any(MissingTarget).ShouldBeFalse();
}
