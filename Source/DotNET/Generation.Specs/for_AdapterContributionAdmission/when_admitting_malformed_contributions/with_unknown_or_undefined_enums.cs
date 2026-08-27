// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_unknown_or_undefined_enums : given.a_contribution
{
    AdapterContributionAdmissionResult _descriptorUnknown = null!;
    AdapterContributionAdmissionResult _descriptorUndefined = null!;
    AdapterContributionAdmissionResult _factUnknown = null!;
    AdapterContributionAdmissionResult _evidenceUndefined = null!;

    void Because()
    {
        _descriptorUnknown = Admit(Descriptor() with { SourceLanguage = AdapterSourceLanguage.Unknown });
        _descriptorUndefined = Admit(Descriptor() with { Category = (AdapterCategory)731 });

        var unknownFacts = EveryFact();
        var artifact = (ArtifactFact)unknownFacts[0];
        unknownFacts[0] = artifact with
        {
            Definition = artifact.Definition with
            {
                Key = artifact.Definition.Key with { Kind = ArtifactKind.Unknown }
            }
        };
        _factUnknown = Admit(contribution: Contribution(unknownFacts));

        var undefinedEvidence = EveryFact();
        undefinedEvidence[0] = undefinedEvidence[0] with
        {
            Evidence = Evidence() with { Strength = (EvidenceStrength)731 }
        };
        _evidenceUndefined = Admit(contribution: Contribution(undefinedEvidence));
    }

    [Fact] void should_reject_unknown_descriptor_enums() => AssertRejected(_descriptorUnknown, AdapterContributionAdmissionDiagnosticCode.UnknownEnumValue);
    [Fact] void should_reject_undefined_descriptor_enums() => AssertRejected(_descriptorUndefined, AdapterContributionAdmissionDiagnosticCode.UndefinedEnumValue);
    [Fact] void should_reject_unknown_fact_enums() => AssertRejected(_factUnknown, AdapterContributionAdmissionDiagnosticCode.UnknownEnumValue);
    [Fact] void should_reject_undefined_evidence_enums() => AssertRejected(_evidenceUndefined, AdapterContributionAdmissionDiagnosticCode.UndefinedEnumValue);

    static void AssertRejected(AdapterContributionAdmissionResult result, AdapterContributionAdmissionDiagnosticCode code)
    {
        result.Snapshot.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(code);
    }
}
