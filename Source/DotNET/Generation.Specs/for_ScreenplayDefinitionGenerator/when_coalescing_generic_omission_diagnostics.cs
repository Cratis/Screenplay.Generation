// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_coalescing_generic_omission_diagnostics : given.omission_groups
{
    GeneratedScreenplayDefinition _grouped;
    GeneratedScreenplayDefinition _ungrouped;
    GeneratedScreenplayDefinition _permuted;
    GeneratedScreenplayDefinition _contributions;
    void Because()
    {
        var first = Member("first");
        var second = Member("second");
        var group = Group(first, second);
        GenerationFact[] facts = [first, second, group];
        _grouped = Generate(facts);
        _ungrouped = Generate([first, second]);
        _permuted = Generate([group with { Definition = group.Definition with { CoveredFacts = [second.Id, first.Id] } }, second, first]);
        _contributions = Generator.Generate([new AdapterContribution { Adapter = Adapter, Facts = facts }], new() { Domain = "Diagnostics" });
    }
    [Fact] void should_replace_generic_noise_with_one_explanation() => _grouped.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual(["OBS_GROUP"]);
    [Fact] void should_preserve_emitted_source() => _grouped.Source.ShouldEqual(_ungrouped.Source);
    [Fact] void should_keep_the_group_as_provenance() => _grouped.AdapterRun.Facts.Single(record => record.Fact.Id == Id("group")).Disposition.ShouldEqual(GenerationFactDisposition.ProvenanceOnly);
    [Fact] void should_keep_each_covered_record_omitted() => _grouped.AdapterRun.Facts.Where(record => record.Fact.Id != Id("group")).All(record => record.Disposition == GenerationFactDisposition.OmittedWithDiagnostic).ShouldBeTrue();
    [Fact] void should_keep_generic_diagnostics_on_each_covered_record() => _grouped.AdapterRun.Facts.Where(record => record.Fact.Id != Id("group")).All(record => record.Diagnostics.Single().Code == GenerationDiagnosticCodes.OmittedGenerationFact).ShouldBeTrue();
    [Fact] void should_include_all_group_and_covered_ids() => _grouped.Diagnostics.Single().Facts.Select(id => id.Value).ShouldEqual([Id("first").Value, Id("group").Value, Id("second").Value]);
    [Fact] void should_keep_complete_evidence() => _grouped.Diagnostics.Single().Lineage.Evidence.Length.ShouldEqual(3);
    [Fact] void should_keep_permutations_byte_identical() => JsonSerializer.Serialize(_permuted).ShouldEqual(JsonSerializer.Serialize(_grouped));
    [Fact] void should_coalesce_the_contribution_overload() => JsonSerializer.Serialize(_contributions.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_grouped.Diagnostics));
    [Fact] void should_leave_ungrouped_contributions_unchanged() => Generator.Generate([new AdapterContribution { Adapter = Adapter, Facts = [Member("first"), Member("second")] }], new() { Domain = "Diagnostics" }).Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_add_diagnostics_for_group_metadata() => _grouped.AdapterRun.Facts.Single(record => record.Fact.Id == Id("group")).Diagnostics.ShouldBeEmpty();
}
