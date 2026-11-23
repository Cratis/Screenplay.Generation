// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_concept_only_binding_policies : given.a_contribution
{
    AdapterContributionAdmissionResult _result;
    TypeUseBindingPolicyFact _policy;

    void Establish() => _policy = BindingPolicy();
    void Because() => _result = Admit(contribution: Contribution([ObservedType(), _policy]));

    [Fact] void should_admit_the_producer_owned_opt_in() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_definition() => ReferenceEquals(Frozen().Definition, _policy.Definition).ShouldBeFalse();
    [Fact] void should_freeze_the_observation_reference() => ReferenceEquals(Frozen().Definition.TypeUse, _policy.Definition.TypeUse).ShouldBeFalse();
    [Fact] void should_preserve_the_observation_reference() => Frozen().Definition.TypeUse.ShouldEqual(ObservedType().Id);
    [Fact] void should_require_the_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.TypeUseBindingPolicy)]), contribution: Contribution([ObservedType(), _policy])).IsAdmitted.ShouldBeFalse();
    [Fact] void should_admit_a_declaration_input_policy() => Admit(contribution: Contribution([InputType(), _policy with { Definition = _policy.Definition with { TypeUse = InputType().Id } }])).IsAdmitted.ShouldBeTrue();

    TypeUseBindingPolicyFact Frozen() => _result.Snapshot.Facts.OfType<TypeUseBindingPolicyFact>().Single();
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
