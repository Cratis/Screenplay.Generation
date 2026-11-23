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
    [Fact] void should_freeze_the_member() => ReferenceEquals(Frozen().Definition.Member, _policy.Definition.Member).ShouldBeFalse();
    [Fact] void should_freeze_the_warning() => ReferenceEquals(Frozen().Definition.Diagnostic, _policy.Definition.Diagnostic).ShouldBeFalse();
    [Fact] void should_freeze_warning_source() => ReferenceEquals(Frozen().Definition.Diagnostic.Source, _policy.Definition.Diagnostic.Source).ShouldBeFalse();
    [Fact] void should_preserve_warning_links() => Frozen().Definition.Diagnostic.Facts.ShouldContainOnly(ObservedType().Id);
    [Fact] void should_require_the_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.TypeUseBindingPolicy)]), contribution: Contribution([ObservedType(), _policy])).IsAdmitted.ShouldBeFalse();

    TypeUseBindingPolicyFact Frozen() => _result.Snapshot.Facts.OfType<TypeUseBindingPolicyFact>().Single();
    static ArtifactMemberTypeUseFact ObservedType() => EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
}
