// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_one_omission_occurrence_has_incompatible_payloads : given.occurrence_omissions
{
    GenerationDiagnostic[] _diagnostics;
    GenerationDiagnostic[] _reversed;
    OccurrenceOmissionDiagnosticFact _preferred;
    void Because()
    {
        var original = Candidate("original");
        var code = Changed(Candidate("code"), original.Definition.Diagnostic with { Code = "OTHER0044" });
        var severity = Changed(Candidate("severity"), original.Definition.Diagnostic with { Severity = GenerationDiagnosticSeverity.Warning });
        var outcome = Changed(Candidate("outcome"), original.Definition.Diagnostic with { Outcome = GenerationDiagnosticOutcome.Unknown });
        var message = Changed(Candidate("message"), original.Definition.Diagnostic with { Message = "A different omission" });
        _preferred = Candidate("preferred", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        var facts = CandidateFacts(original, code, severity, outcome, message, _preferred);
        _diagnostics = Omissions(Derive(facts));
        _reversed = Omissions(Derive([.. facts.Reverse()]));
    }
    [Fact] void should_retain_every_semantic_partition_and_one_conflict() => _diagnostics.Length.ShouldEqual(6);
    [Fact] void should_not_hide_unrelated_codes() => _diagnostics.Count(diagnostic => diagnostic.Code == "OTHER0044").ShouldEqual(1);
    [Fact] void should_locate_the_conflict_at_the_canonical_preferred_evidence() => Conflict().Source.ShouldEqual(_preferred.Evidence.Source);
    [Fact] void should_mark_the_conflict_explicitly() => Conflict().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Conflict);
    [Fact] void should_keep_all_conflicting_contributors_in_lineage() => Conflict().Lineage.Inputs.Length.ShouldEqual(12);
    [Fact] void should_keep_conflict_and_representatives_order_independent() => JsonSerializer.Serialize(_reversed).ShouldEqual(JsonSerializer.Serialize(_diagnostics));

    GenerationDiagnostic Conflict() => _diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingOccurrenceOmissionDiagnostic);
    static OccurrenceOmissionDiagnosticFact Changed(OccurrenceOmissionDiagnosticFact candidate, GenerationDiagnostic payload) => candidate with
    {
        Definition = candidate.Definition with { Diagnostic = payload with { Source = candidate.Evidence.Source, Facts = candidate.Definition.Diagnostic.Facts } }
    };
}
