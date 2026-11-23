// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_splitting_source_owner_candidate_placements : given.mixed_slice_generation
{
    GeneratedScreenplayDefinition _direct = null!;
    GeneratedScreenplayDefinition _snapshot = null!;
    GeneratedScreenplayDefinition _reversed = null!;

    void Because()
    {
        var write = AsCandidates(Write);
        var read = AsCandidates(Read);
        _direct = Generator.Generate([Contribution(write), new AdapterContribution { Adapter = OtherAdapter, Facts = read }], Options);
        _snapshot = GenerateRun(write, read);
        _reversed = GenerateRun([.. write.Reverse()], [.. read.Reverse()], reverse: true);
    }

    [Fact] void should_compile_and_canonically_reprint() => _snapshot.IsSuccess.ShouldBeTrue();
    [Fact] void should_agree_across_both_entry_paths() => _direct.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_keep_the_entire_snapshot_stable_under_reversed_order() => AdapterRunProjection(_snapshot.AdapterRun).ShouldEqual(AdapterRunProjection(_reversed.AdapterRun));
    [Fact] void should_lower_the_normalized_outputs() => Normalized(_snapshot).Count(record => record.Disposition == GenerationFactDisposition.Lowered).ShouldEqual(2);
    [Fact] void should_flatten_candidate_lineage_to_admitted_facts() => Normalized(_snapshot).All(record => LineageOf(record).Inputs.All(id => RunOf(_snapshot).Facts.Any(input => input.Fact.Id == id))).ShouldBeTrue();
    [Fact] void should_retain_candidates_as_provenance() => RunOf(_snapshot).Facts.Where(record => record.Fact is ArtifactSourcePlacementCandidateFact).All(record => record.Disposition == GenerationFactDisposition.ProvenanceOnly).ShouldBeTrue();
    [Fact] void should_not_treat_the_repair_warning_as_a_conflict() => RunOf(_snapshot).Derivation.Facts.Any(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeFalse();
}
