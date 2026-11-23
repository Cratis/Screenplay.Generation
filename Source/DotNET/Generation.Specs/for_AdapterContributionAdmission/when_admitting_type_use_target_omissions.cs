// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_type_use_target_omissions : given.a_contribution
{
    AdapterContributionAdmissionResult _valid = null!;
    AdapterContributionAdmissionResult _missingUse = null!;
    AdapterContributionAdmissionResult _otherProducer = null!;
    AdapterContributionAdmissionResult _otherMember = null!;
    AdapterContributionAdmissionResult _invalidDiagnostic = null!;
    AdapterContributionAdmissionResult _missingDiagnostic = null!;
    AdapterContributionAdmissionResult _undeclaredCapability = null!;
    ArtifactMemberTypeUseFact _typeUse = null!;
    TypeUseTargetOmittedFact _omission = null!;
    GenerationFact[] _facts = [];

    void Establish()
    {
        _typeUse = EveryFact().OfType<ArtifactMemberTypeUseFact>().Single();
        _omission = new TypeUseTargetOmittedFact
        {
            Id = Id("omission"), Subject = ArtifactSubject, Evidence = Evidence(),
            Definition = new TypeUseTargetOmittedDefinition
            {
                Member = _typeUse.Definition.Member,
                Diagnostic = new GenerationDiagnostic
                {
                    Code = "atomic.TargetOmitted", Severity = GenerationDiagnosticSeverity.Warning,
                    Message = "No target is contributed", Source = Source(), Facts = [_typeUse.Id], Subject = ArtifactSubject
                }
            }
        };
    }

    void Because()
    {
        _facts = [_typeUse, _omission];
        _valid = AdmitOmission(_facts);
        _missingUse = AdmitOmission(_omission);
        _otherProducer = AdmitOmission(_typeUse with { Evidence = _typeUse.Evidence with { Adapter = new AdapterIdentity { Id = "other", Version = "1.0.0" } } }, _omission);
        _otherMember = AdmitOmission(_typeUse, _omission with { Definition = _omission.Definition with { Member = MemberKey("unrelated") } });
        _invalidDiagnostic = AdmitOmission(_typeUse, _omission with { Definition = _omission.Definition with { Diagnostic = _omission.Definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Unknown, Facts = [Id("missing")] } } });
        _missingDiagnostic = AdmitOmission(_typeUse, _omission with { Definition = _omission.Definition with { Diagnostic = null! } });
        _undeclaredCapability = Admit(
            descriptor: Descriptor([.. AllCapabilities().Where(capability => capability != GenerationFactCapability.TypeUseTargetOmitted)]),
            contribution: Contribution([_typeUse, _omission]));
        _facts[1] = _typeUse;
    }

    [Fact] void should_admit_the_typed_omission() => _valid.IsAdmitted.ShouldBeTrue();
    [Fact] void should_freeze_the_diagnostic_references() => _valid.Snapshot.Facts.OfType<TypeUseTargetOmittedFact>().Single().Definition.Diagnostic.Facts.ShouldContainOnly(_typeUse.Id);
    [Fact] void should_freeze_the_diagnostic_location() => ReferenceEquals(_valid.Snapshot.Facts.OfType<TypeUseTargetOmittedFact>().Single().Definition.Diagnostic.Source, _omission.Definition.Diagnostic.Source).ShouldBeFalse();
    [Fact] void should_reject_a_missing_authored_type_use() => _missingUse.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_another_producer() => _otherProducer.IsAdmitted.ShouldBeFalse();
    [Fact] void should_reject_a_different_member() => _otherMember.IsAdmitted.ShouldBeFalse();
    [Fact] void should_validate_the_diagnostic_and_its_references() => _invalidDiagnostic.IsAdmitted.ShouldBeFalse();
    [Fact] void should_require_the_diagnostic() => _missingDiagnostic.IsAdmitted.ShouldBeFalse();
    [Fact] void should_require_the_emitted_capability() => _undeclaredCapability.IsAdmitted.ShouldBeFalse();

    static AdapterContributionAdmissionResult AdmitOmission(params GenerationFact[] facts) => Admit(
        descriptor: Descriptor(),
        contribution: Contribution(facts));
}
