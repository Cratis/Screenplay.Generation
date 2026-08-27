// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_deriving_an_exact_cross_adapter_type_use : given.a_derivation
{
    GenerationDerivationSnapshot _result = null!;

    void Because() => _result = Derive(
        TypeUse("customerCode", ConceptSubject, "type-use", TypeUseShapeKind.Optional, TypeUseShapeKind.Named),
        Concept(ConceptSubject, "customer-code"),
        MemberDeclaration("customerCode", 0, "member"),
        CommandDeclaration());

    [Fact] void should_execute_the_stable_rule_version() => _result.Rules.Single().Rule.ShouldEqual(new GenerationDerivationRuleIdentity { Id = "cratis.screenplay.type-use-binding", Version = "1.0.0" });
    [Fact] void should_derive_one_granular_binding_fact() => _result.Facts.Select(record => record.Fact.GetType()).ShouldContainOnly(typeof(TypeUseBindingFact));
    [Fact] void should_not_repeat_a_complete_artifact_fact() => _result.Facts.Any(record => record.Fact is ArtifactFact).ShouldBeFalse();
    [Fact] void should_bind_the_exact_member_to_the_exact_concept_role() => Binding().Definition.ShouldEqual(new TypeUseBindingDefinition { Member = Member("customerCode"), Target = new ArtifactKey { Subject = ConceptSubject, Kind = ArtifactKind.Concept } });
    [Fact] void should_identify_the_derivation_producer() => Record().Lineage!.Producer.ShouldEqual(_result.Rules.Single().Rule);
    [Fact] void should_reference_every_canonical_input_fact() => Record().Lineage!.Inputs.Select(id => id.Value).ShouldEqual("application:command", "application:member", "application:type-use", "concepts:customer-code");
    [Fact] void should_retain_every_input_evidence() => Record().Lineage!.Evidence.Length.ShouldEqual(4);
    [Fact] void should_leave_the_derived_disposition_for_generation() => Record().Disposition.ShouldEqual(GenerationFactDisposition.Unknown);
    [Fact] void should_not_report_derivation_loss() => _result.Diagnostics.ShouldBeEmpty();

    GenerationFactRecord Record() => _result.Facts.Single();

    TypeUseBindingFact Binding() => (TypeUseBindingFact)Record().Fact;
}
