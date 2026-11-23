// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_occurrence_omission_candidates : given.occurrence_candidates
{
    OccurrenceOmissionDiagnosticFact _candidate;
    AdapterContributionAdmissionResult _result;
    void Establish() => _candidate = Candidate("registration", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
    void Because() => _result = AdmitOccurrence(_candidate);
    [Fact] void should_admit_a_standalone_local_candidate_without_other_producers() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_keep_the_concrete_fact() => _result.Snapshot.Facts.OfType<OccurrenceOmissionDiagnosticFact>().Count().ShouldEqual(1);
    [Fact] void should_allow_information_and_all_defined_outcomes()
    {
        foreach (var outcome in Enum.GetValues<GenerationDiagnosticOutcome>())
        {
            AdmitOccurrence(_candidate with { Definition = _candidate.Definition with { Diagnostic = _candidate.Definition.Diagnostic with { Outcome = outcome } } }).IsAdmitted.ShouldBeTrue();
        }
    }
    [Fact] void should_allow_supported_roles_other_than_document() => AdmitOccurrence(_candidate with { Definition = _candidate.Definition with { Occurrence = _candidate.Definition.Occurrence with { Kind = ArtifactKind.ReadModel } } }).IsAdmitted.ShouldBeTrue();
    [Fact] void should_allow_matching_optional_method_support() => AdmitOccurrence(_candidate, Conditional(_candidate)).IsAdmitted.ShouldBeTrue();
    [Fact] void should_reject_a_method_support_scope_mismatch()
    {
        var support = Conditional(_candidate);
        support = support with { Definition = support.Definition with { Alternatives = [new() { Method = Method, Scope = new() { Store = new() { Value = "symbol://stores/Other" } } }] } };
        AdmitOccurrence(_candidate, support).IsAdmitted.ShouldBeFalse();
    }
    [Fact] void should_require_the_declared_capability() => AdapterContributionAdmission.Admit(OmissionDescriptor(_candidate) with { EmittedFactCapabilities = [GenerationFactCapability.DeclarationInputMemberDeclaration] }, new() { Adapter = _candidate.Evidence.Adapter, Facts = CandidateFacts(_candidate) }, new OmissionAuthority()).IsAdmitted.ShouldBeFalse();
}
