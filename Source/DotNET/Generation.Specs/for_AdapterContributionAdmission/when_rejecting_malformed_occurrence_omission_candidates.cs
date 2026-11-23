// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_rejecting_malformed_occurrence_omission_candidates : given.occurrence_candidates
{
    OccurrenceOmissionDiagnosticFact _candidate;
    OccurrenceOmissionDiagnosticDefinition _definition;
    void Establish()
    {
        _candidate = Candidate("registration");
        _definition = _candidate.Definition;
    }
    [Fact] void should_reject_missing_definitions() => Reject(null!);
    [Fact] void should_reject_missing_families() => Reject(_definition with { Family = null! });
    [Fact] void should_reject_empty_families() => Reject(_definition with { Family = new() { Value = "" } });
    [Fact] void should_reject_non_normalized_families() => Reject(_definition with { Family = new() { Value = " document-semantics " } });
    [Fact] void should_reject_missing_occurrences() => Reject(_definition with { Occurrence = null! });
    [Fact] void should_reject_malformed_occurrence_subjects() => Reject(_definition with { Occurrence = _definition.Occurrence with { Subject = new() { Value = "relative" } } });
    [Fact] void should_reject_subject_mismatches() => Reject(_definition with { Occurrence = _definition.Occurrence with { Subject = Method } });
    [Fact] void should_reject_unknown_roles() => Reject(_definition with { Occurrence = _definition.Occurrence with { Kind = ArtifactKind.Unknown } });
    [Fact] void should_reject_undefined_roles() => Reject(_definition with { Occurrence = _definition.Occurrence with { Kind = (ArtifactKind)999 } });
    [Fact] void should_reject_malformed_scopes() => Reject(_definition with { Occurrence = _definition.Occurrence with { Scope = new() { Store = new() { Value = "relative" } } } });
    [Fact] void should_reject_unknown_policies() => Reject(_definition with { Policy = OccurrenceOmissionDiagnosticPolicy.Unknown });
    [Fact] void should_reject_undefined_policies() => Reject(_definition with { Policy = (OccurrenceOmissionDiagnosticPolicy)999 });
    [Fact] void should_reject_unknown_preferences() => Reject(_definition with { Preference = OmissionDiagnosticEvidencePreference.Unknown });
    [Fact] void should_reject_undefined_preferences() => Reject(_definition with { Preference = (OmissionDiagnosticEvidencePreference)999 });
    [Fact] void should_reject_missing_payloads() => Reject(_definition with { Diagnostic = null! });
    [Fact] void should_reject_empty_messages() => RejectPayload(_definition.Diagnostic with { Message = "" });
    [Fact] void should_reject_unknown_severities() => RejectPayload(_definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Unknown });
    [Fact] void should_reject_undefined_outcomes() => RejectPayload(_definition.Diagnostic with { Outcome = (GenerationDiagnosticOutcome)999 });
    [Fact] void should_reject_unlocated_payloads() => RejectPayload(_definition.Diagnostic with { Source = null });
    [Fact] void should_reject_payload_subject_mismatches() => RejectPayload(_definition.Diagnostic with { Subject = Method });
    [Fact] void should_reject_payload_evidence_source_mismatches() => RejectPayload(_definition.Diagnostic with { Source = Invocation });
    [Fact] void should_reject_empty_local_support() => RejectPayload(_definition.Diagnostic with { Facts = [] });
    [Fact] void should_reject_missing_local_support() => RejectPayload(_definition.Diagnostic with { Facts = [new() { Value = "registration:missing" }] });
    [Fact] void should_reject_foreign_support() => RejectPayload(_definition.Diagnostic with { Facts = [new() { Value = "another:observation" }] });
    [Fact] void should_reject_self_support() => RejectPayload(_definition.Diagnostic with { Facts = [_candidate.Id] });
    [Fact] void should_reject_caller_owned_lineage() => RejectPayload(_definition.Diagnostic with { Lineage = new() { Producer = new() { Id = "caller", Version = "1.0.0" }, Inputs = [], Evidence = [] } });
    [Fact] void should_reject_unauthorized_sources()
    {
        var source = _candidate.Evidence.Source with { Path = "unauthorized.cs" };
        AdmitOccurrence(_candidate with { Evidence = _candidate.Evidence with { Source = source }, Definition = _definition with { Diagnostic = _definition.Diagnostic with { Source = source } } }).IsAdmitted.ShouldBeFalse();
    }

    void RejectPayload(GenerationDiagnostic payload) => Reject(_definition with { Diagnostic = payload });
    void Reject(OccurrenceOmissionDiagnosticDefinition definition) => AdmitOccurrence(_candidate with { Definition = definition }).IsAdmitted.ShouldBeFalse();
}
