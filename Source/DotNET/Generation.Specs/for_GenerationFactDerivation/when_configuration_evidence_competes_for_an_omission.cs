// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_configuration_evidence_competes_for_an_omission : given.occurrence_omissions
{
    OccurrenceOmissionDiagnosticFact _identity;
    GenerationDiagnostic _diagnostic;
    void Because()
    {
        _identity = Candidate("z-identity", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        _identity = _identity with { Evidence = _identity.Evidence with { Strength = EvidenceStrength.Configured } };
        _diagnostic = Omissions(Derive(CandidateFacts(Candidate("a-use"), Candidate("b-registration", OmissionDiagnosticEvidencePreference.ExplicitRegistration), _identity))).Single();
    }
    [Fact] void should_prefer_identity_configuration_over_registration_and_use_site() => _diagnostic.Source.ShouldEqual(_identity.Definition.Diagnostic.Source);
    [Fact] void should_retain_the_lower_preference_evidence() => _diagnostic.Lineage.Inputs.Length.ShouldEqual(6);
}
