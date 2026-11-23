// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_a_located_omission_conflict : for_GenerationFactDerivation.given.occurrence_omissions
{
    GeneratedScreenplayDefinition _result;
    GeneratedScreenplayDefinition _reversed;
    SourceRange _location;
    void Because()
    {
        var first = Candidate("first", OmissionDiagnosticEvidencePreference.IdentityConfiguration);
        var second = Candidate("second");
        second = second with { Definition = second.Definition with { Diagnostic = second.Definition.Diagnostic with { Code = "ANOTHER0044", Outcome = GenerationDiagnosticOutcome.Conflict } } };
        _location = first.Evidence.Source;
        var facts = CandidateFacts(first, second);
        var generator = new ScreenplayDefinitionGenerator();
        _result = generator.Generate(OmissionSnapshot(facts), new() { Domain = "Diagnostics" });
        var snapshot = OmissionSnapshot(facts.Reverse());
        _reversed = generator.Generate(snapshot with { Adapters = [.. snapshot.Adapters.Reverse()] }, new() { Domain = "Diagnostics" });
    }
    [Fact] void should_keep_both_original_payloads() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == "OMIT0044" || diagnostic.Code == "ANOTHER0044").ShouldEqual(2);
    [Fact] void should_emit_one_located_conflict() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingOccurrenceOmissionDiagnostic).Source.ShouldEqual(_location);
    [Fact] void should_preserve_all_conflicting_evidence_through_final_copying() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingOccurrenceOmissionDiagnostic).Lineage.Inputs.Length.ShouldEqual(4);
    [Fact] void should_classify_both_candidates_as_conflicted() => _result.AdapterRun.Facts.Where(record => record.Fact is OccurrenceOmissionDiagnosticFact).All(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();
    [Fact] void should_keep_the_entire_output_order_independent() => JsonSerializer.Serialize(_reversed).ShouldEqual(JsonSerializer.Serialize(_result));
}
