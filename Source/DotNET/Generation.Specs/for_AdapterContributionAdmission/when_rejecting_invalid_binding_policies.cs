// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_invalid_binding_policies : given.a_contribution
{
    static ArtifactMemberTypeUseFact ObservedType() => EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
    static AdapterContributionAdmissionResult AdmitPolicy(TypeUseBindingPolicyFact policy) => Admit(contribution: Contribution([ObservedType(), policy]));
    static TypeUseBindingPolicyFact WithDefinition(TypeUseBindingPolicyDefinition definition) => BindingPolicy() with { Definition = definition };
    static TypeUseBindingPolicyFact WithWarning(GenerationDiagnostic diagnostic) => WithDefinition(BindingPolicy().Definition with { Diagnostic = diagnostic });

    [Fact] void should_reject_a_dangling_policy() => Admit(contribution: Contribution([BindingPolicy()])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_duplicate_policies() => Admit(contribution: Contribution([ObservedType(), BindingPolicy(), BindingPolicy() with { Id = Id("duplicate-policy") }])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_producer() => AdmitPolicy(BindingPolicy() with { Evidence = Evidence() with { Adapter = new() { Id = "other", Version = "1" } } }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_member() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { Member = MemberKey("other") })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_subject() => AdmitPolicy(BindingPolicy() with { Subject = ExternalSubject }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_non_concept_kind() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { AllowedTargetKind = ArtifactKind.Event })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_unknown_kind() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { AllowedTargetKind = ArtifactKind.Unknown })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_undefined_kind() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { AllowedTargetKind = (ArtifactKind)999 })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_unknown_fallback() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { Fallback = TypeUseBindingFallback.Unknown })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_undefined_fallback() => AdmitPolicy(WithDefinition(BindingPolicy().Definition with { Fallback = (TypeUseBindingFallback)999 })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_an_error_template() => AdmitPolicy(WithWarning(BindingPolicy().Definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Error })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_warning() => AdmitPolicy(WithWarning(null!)).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_missing_definition() => AdmitPolicy(BindingPolicy() with { Definition = null! }).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_unknown_warning_links() => AdmitPolicy(WithWarning(BindingPolicy().Definition.Diagnostic with { Facts = [Id("missing")] })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_warning_subject() => AdmitPolicy(WithWarning(BindingPolicy().Definition.Diagnostic with { Subject = ExternalSubject })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_foreign_warning_source() => AdmitPolicy(WithWarning(BindingPolicy().Definition.Diagnostic with { Source = Source() with { Path = "Other.cs" } })).IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_type_without_an_exact_subject() => Admit(contribution: Contribution([ObservedType() with { Definition = ObservedType().Definition with { Type = ObservedType().Definition.Type with { ObservedTypeSubject = null } } }, BindingPolicy()])).IsAdmitted.ShouldBeFalse();
}
