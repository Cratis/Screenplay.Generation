// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_splitting_placements_with_reused_fact_ids : given.mixed_slice_generation
{
    GeneratedScreenplayDefinition _direct = null!;
    GeneratedScreenplayDefinition _snapshot = null!;
    GeneratedScreenplayDefinition _reversed = null!;
    GeneratedScreenplayDefinition _candidates = null!;
    GeneratedScreenplayDefinition _reversedCandidates = null!;

    void Establish()
    {
        Write = ReuseIds(Write);
        Read = ReuseIds(Read);
    }

    void Because()
    {
        _direct = Generator.Generate([Contribution(Write), new AdapterContribution { Adapter = OtherAdapter, Facts = Read }], Options);
        _snapshot = GenerateRun(Write, Read);
        _reversed = GenerateRun([.. Write.Reverse()], [.. Read.Reverse()], reverse: true);
        _candidates = GenerateRun(AsCandidates(Write), AsCandidates(Read));
        _reversedCandidates = GenerateRun([.. AsCandidates(Write).Reverse()], [.. AsCandidates(Read).Reverse()], reverse: true);
    }

    [Fact] void should_agree_across_both_entry_paths() => _direct.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_emit_both_split_slices() => _snapshot.Source.Contains("slice StateChange Write", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_emit_the_state_view_slice() => _snapshot.Source.Contains("slice StateView View", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_complete_snapshot_stable() => AdapterRunProjection(_snapshot.AdapterRun).ShouldEqual(AdapterRunProjection(_reversed.AdapterRun));
    [Fact] void should_keep_candidate_snapshots_stable() => AdapterRunProjection(_candidates.AdapterRun).ShouldEqual(AdapterRunProjection(_reversedCandidates.AdapterRun));
    [Fact] void should_report_conflicting_base_identities() => RunOf(_snapshot).Facts.Count(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldEqual(4);
    [Fact] void should_retain_the_repair_warning() => _snapshot.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit).ShouldEqual(1);
    [Fact] void should_lower_both_effective_placements() => Normalized(_snapshot).Count(record => record.Disposition == GenerationFactDisposition.Lowered).ShouldEqual(2);
    [Fact] void should_retain_evidence_from_both_adapters() => Normalized(_snapshot).All(record => LineageOf(record).Evidence.Select(evidence => evidence.Adapter).Distinct().Count() == 2).ShouldBeTrue();
    [Fact] void should_lower_both_candidate_placements() => Normalized(_candidates).Count(record => record.Disposition == GenerationFactDisposition.Lowered).ShouldEqual(2);
    [Fact] void should_expand_reused_candidate_lineage() => Normalized(_candidates).All(record => LineageOf(record).Inputs.All(id => RunOf(_candidates).Facts.Any(input => input.Fact.Id == id))).ShouldBeTrue();
    [Fact] void should_retain_both_candidate_producers_in_lineage() => Normalized(_candidates).All(record => LineageOf(record).Evidence.Select(evidence => evidence.Adapter).Distinct().Count() == 2).ShouldBeTrue();

    static GenerationFact[] ReuseIds(GenerationFact[] facts) => [.. facts.Select(fact => fact with
    {
        Id = new() { Value = fact is ArtifactPlacementFact ? "shared:placement" : "shared:artifact" }
    })];
}
