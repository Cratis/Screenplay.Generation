// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_yielded_concept_facets_are_orphaned : given.yielding_concepts
{
    static readonly string[] _losers = ["critter-stack:concept-enum", "critter-stack:representation-enum", "critter-stack:attribute-enum", "critter-stack:validation-enum"];

    GeneratedScreenplayDefinition[] _results = [];

    void Because()
    {
        var other = new AdapterIdentity { Id = "other", Version = "1.0.0" };
        var losing = Evidence(Adapter);
        var foreign = Evidence(other);
        GenerationFact[] first =
        [
            .. Command(), .. Candidate("enum", true),
            new ConceptAttributeFact
            {
                Id = new FactId { Value = "critter-stack:attribute-enum" }, Subject = Target, Evidence = losing,
                Definition = new ConceptAttributeDefinition { Concept = Target, Name = "sensitive", Reason = "Permissions" }
            },
            Validation("critter-stack:validation-enum", Target, "positive", "value > 0")
        ];
        GenerationFact[] second =
        [
            .. Candidate("vogen", false, true, other),
            new ConceptAttributeFact
            {
                Id = new FactId { Value = "other:attribute-foreign" }, Subject = Target, Evidence = foreign,
                Definition = new ConceptAttributeDefinition { Concept = Target, Name = "audited", Reason = "Other producer" }
            }
        ];
        var firstRun = Completed(Adapter, first);
        var secondRun = Completed(other, second);
        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _results = [Generator.Generate(Snapshot(firstRun, secondRun), options), Generator.Generate(Snapshot(secondRun, firstRun), options)];
    }

    [Fact] void should_omit_every_losing_facet_with_a_diagnostic() => _results.All(result => _losers.All(id => RecordOf(result, id).Disposition == GenerationFactDisposition.OmittedWithDiagnostic)).ShouldBeTrue();
    [Fact] void should_explain_every_losing_facet_with_the_yield_warning() => _results.All(result => _losers.All(id => RecordOf(result, id).Diagnostics.Count(IsWarning) == 1)).ShouldBeTrue();
    [Fact] void should_share_one_full_yield_payload() => _results.All(result => _losers.Select(id => Projection(RecordOf(result, id).Diagnostics.Where(IsWarning))).Distinct().Count() == 1).ShouldBeTrue();
    [Fact] void should_not_add_generic_omissions_to_the_losing_facets() => _results.All(result => _losers.All(id => RecordOf(result, id).Diagnostics.All(diagnostic => diagnostic.Code != GenerationDiagnosticCodes.OmittedGenerationFact))).ShouldBeTrue();
    [Fact] void should_not_add_generic_omissions_to_any_list() => _results.All(result => Of(result.Diagnostics, GenerationDiagnosticCodes.OmittedGenerationFact).All(diagnostic => !diagnostic.Facts.Any(link => _losers.Contains(link.Value))) && Of(result.AdapterRun.Diagnostics, GenerationDiagnosticCodes.OmittedGenerationFact).All(diagnostic => !diagnostic.Facts.Any(link => _losers.Contains(link.Value)))).ShouldBeTrue();
    [Fact] void should_report_the_yield_once() => _results.All(result => result.Diagnostics.Count(IsWarning) == 1 && result.AdapterRun.Diagnostics.Count(IsWarning) == 1).ShouldBeTrue();
    [Fact] void should_not_explain_the_winner_with_the_yield() => _results.All(result => RecordOf(result, "other:representation-vogen").Diagnostics.All(diagnostic => !IsWarning(diagnostic))).ShouldBeTrue();
    [Fact] void should_not_explain_a_foreign_fact_with_the_yield() => _results.All(result => RecordOf(result, "other:attribute-foreign").Diagnostics.All(diagnostic => !IsWarning(diagnostic))).ShouldBeTrue();
    [Fact] void should_keep_identical_run_diagnostics_in_both_orders() => Projection(_results[1].AdapterRun.Diagnostics).ShouldEqual(Projection(_results[0].AdapterRun.Diagnostics));
    [Fact] void should_keep_identical_sources_in_both_orders() => _results[1].Source.ShouldEqual(_results[0].Source);

    static GenerationDiagnostic[] Of(IEnumerable<GenerationDiagnostic> diagnostics, string code) =>
        [.. diagnostics.Where(diagnostic => diagnostic.Code == code)];

    static string Projection(IEnumerable<GenerationDiagnostic> diagnostics) => AdapterRunProjection(diagnostics.ToArray());

    static GenerationFactRecord RecordOf(GeneratedScreenplayDefinition result, string id) =>
        result.AdapterRun.Facts.Single(record => record.Fact.Id.Value == id);

    static Evidence Evidence(AdapterIdentity adapter) => new() { Adapter = adapter, Strength = EvidenceStrength.Exact };
}
