// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_cross_producer_omission_diagnostics : for_GenerationFactDerivation.given.occurrence_omissions
{
    GeneratedScreenplayDefinition _forward;
    GeneratedScreenplayDefinition _reversed;
    GeneratedScreenplayDefinition _contributions;
    void Because()
    {
        var facts = CandidateFacts(Candidate("registration", OmissionDiagnosticEvidencePreference.IdentityConfiguration), Candidate("projection"), Candidate("query"), Candidate("api"));
        var generator = new ScreenplayDefinitionGenerator();
        _forward = generator.Generate(OmissionSnapshot(facts), new() { Domain = "Diagnostics" });
        var reversed = OmissionSnapshot(facts.Reverse());
        reversed = reversed with { Adapters = [.. reversed.Adapters.Reverse()] };
        _reversed = generator.Generate(reversed, new() { Domain = "Diagnostics" });
        _contributions = generator.Generate(Contributions(facts).Reverse().Select(contribution => contribution with { Facts = [.. contribution.Facts.Reverse()] }), new() { Domain = "Diagnostics" });
    }
    [Fact] void should_emit_one_compatible_omission() => _forward.Diagnostics.Count(diagnostic => diagnostic.Code == "OMIT0044").ShouldEqual(1);
    [Fact] void should_keep_all_producers_in_final_diagnostic_lineage() => _forward.Diagnostics.Single(diagnostic => diagnostic.Code == "OMIT0044").Lineage.Evidence.Select(evidence => evidence.Adapter.Id).Distinct().Count().ShouldEqual(4);
    [Fact] void should_keep_the_entire_snapshot_and_source_byte_identical_under_reversed_adapters_and_facts() => JsonSerializer.Serialize(_reversed).ShouldEqual(JsonSerializer.Serialize(_forward));
    [Fact] void should_keep_contribution_diagnostics_identical_under_reversed_adapters() => JsonSerializer.Serialize(_contributions.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_forward.Diagnostics));
    [Fact] void should_preserve_every_metadata_fact_as_provenance() => _forward.AdapterRun.Facts.Where(record => record.Fact is OccurrenceOmissionDiagnosticFact).All(record => record.Disposition == GenerationFactDisposition.ProvenanceOnly).ShouldBeTrue();
    [Fact] void should_not_create_generic_omission_noise_for_metadata() => _forward.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.OmittedGenerationFact && diagnostic.Facts.Any(id => id.Value.EndsWith(":omission", StringComparison.Ordinal))).ShouldBeFalse();
    [Fact] void should_not_infer_omission_intents_from_projected_method_diagnostics()
    {
        var result = new ScreenplayDefinitionGenerator().Generate(OmissionSnapshot([.. Inputs(), .. CandidateFacts(Candidate("independent"))]), new() { Domain = "Diagnostics" });
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "OMIT0044").ShouldEqual(1);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == "OBS0044").ShouldEqual(1);
    }
}
