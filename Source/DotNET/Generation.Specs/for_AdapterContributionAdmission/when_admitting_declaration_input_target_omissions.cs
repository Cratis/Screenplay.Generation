// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_declaration_input_target_omissions : given.declaration_input_target_omissions
{
    AdapterContributionAdmissionResult _valid;
    AdapterContributionAdmissionResult _invalid;
    GenerationFact[] _facts;
    DeclarationInputMemberTargetOmittedFact _omission;

    void Establish()
    {
        _omission = Omission();
        _facts = [ObservedType(), _omission];
    }

    void Because()
    {
        _valid = AdmitOmission(_facts);
        _invalid = AdmitOmission(_omission);
        _facts[1] = _facts[0];
    }

    [Fact] void should_admit_the_declaration_owned_omission() => _valid.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_diagnostic_references() => _valid.Snapshot.Facts.OfType<DeclarationInputMemberTargetOmittedFact>().Single().Definition.Diagnostic.Facts.ShouldContainOnly(ObservedType().Id);
    [Fact] void should_freeze_diagnostic_source() => ReferenceEquals(_valid.Snapshot.Facts.OfType<DeclarationInputMemberTargetOmittedFact>().Single().Definition.Diagnostic.Source, _omission.Definition.Diagnostic.Source).ShouldBeFalse();
    [Fact] void should_freeze_the_member_key() => ReferenceEquals(_valid.Snapshot.Facts.OfType<DeclarationInputMemberTargetOmittedFact>().Single().Definition.Member, _omission.Definition.Member).ShouldBeFalse();
    [Fact] void should_reject_a_dangling_omission() => _invalid.IsAdmitted.ShouldBeFalse();
    [Fact] void should_require_the_capability() => Admit(descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.DeclarationInputMemberTargetOmitted)]), contribution: Contribution([ObservedType(), Omission()])).IsAdmitted.ShouldBeFalse();
}
