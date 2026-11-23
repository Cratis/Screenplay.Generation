// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_binding_policies : given.a_contribution
{
    static ArtifactMemberTypeUseFact ObservedType() => EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
    static AdapterContributionAdmissionResult AdmitPolicy(TypeUseBindingPolicyFact policy) => Admit(contribution: Contribution([ObservedType(), policy]));
    static TypeUseBindingPolicyFact WithDefinition(TypeUseBindingPolicyDefinition definition) => BindingPolicy() with { Definition = definition };

    [Fact] void should_reject_a_dangling_policy() => Admit(contribution: Contribution([BindingPolicy()])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_duplicate_policies() => Admit(contribution: Contribution([ObservedType(), BindingPolicy(), BindingPolicy() with { Id = Id("duplicate-policy") }])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_producer() => AdmitPolicy(BindingPolicy() with { Evidence = Evidence() with { Adapter = new() { Id = "other", Version = "1" } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_reference() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { TypeUse = new() { Value = "other:type-use" } })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_subject() => AdmitPolicy(BindingPolicy() with { Subject = ExternalSubject }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_source() => AdmitPolicy(BindingPolicy() with { Evidence = Evidence() with { Source = Source() with { Path = "Other.cs" } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_non_exact_evidence() => AdmitPolicy(BindingPolicy() with { Evidence = Evidence() with { Strength = EvidenceStrength.Conventional } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_unknown_policy() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { Policy = TypeUseBindingPolicyKind.Unknown })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_undefined_policy() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { Policy = (TypeUseBindingPolicyKind)999 })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_definition() => AdmitPolicy(BindingPolicy() with { Definition = null! }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_reference() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { TypeUse = null! })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_type_without_an_exact_subject() => Admit(contribution: Contribution([ObservedType() with { Definition = ObservedType().Definition with { Type = ObservedType().Definition.Type with { ObservedTypeSubject = null } } }, BindingPolicy()])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_reference_to_another_fact_family() => Admit(contribution: Contribution([EveryFact()[0], WithDefinition(BindingPolicy().Definition with { TypeUse = EveryFact()[0].Id })])).IsAdmitted.ShouldBeFalse();
}
