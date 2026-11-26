// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_reporting_binding_policies : given.a_contribution
{
    AdapterContributionAdmissionResult _result;
    TypeUseBindingPolicyFact _policy;

    void Establish() => _policy = BindingPolicy() with
    {
        Definition = BindingPolicy().Definition with { Policy = TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic }
    };

    void Because() => _result = Admit(contribution: Contribution([ObservedType(), _policy]));

    [Fact] void should_admit_the_reporting_opt_in() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_reporting_policy() => _result.Snapshot.Facts.OfType<TypeUseBindingPolicyFact>().Single().Definition.Policy.ShouldEqual(TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallbackAndMissingConceptDiagnostic);
    [Fact] void should_require_the_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.TypeUseBindingPolicy)]), contribution: Contribution([ObservedType(), _policy])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_admit_a_declaration_input_policy() => Admit(contribution: Contribution([InputType(), _policy with { Definition = _policy.Definition with { TypeUse = InputType().Id } }])).IsAdmitted.ShouldBeTrue();
    [Fact] void should_still_reject_an_unknown_policy() => Admit(contribution: Contribution([ObservedType(), _policy with { Definition = _policy.Definition with { Policy = (TypeUseBindingPolicyKind)3 } }])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_still_reject_the_invalid_default() => Admit(contribution: Contribution([ObservedType(), _policy with { Definition = _policy.Definition with { Policy = TypeUseBindingPolicyKind.Unknown } }])).IsAdmitted.ShouldBeFalse();

    static ArtifactMemberTypeUseFact ObservedType() => EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
    static DeclarationInputMemberTypeUseFact InputType() => new()
    {
        Id = Id("input-type"),
        Subject = ArtifactSubject,
        Evidence = Evidence(),
        Definition = new()
        {
            Member = new() { Declaration = ArtifactSubject, Name = "input" },
            Type = ObservedType().Definition.Type
        }
    };
}
