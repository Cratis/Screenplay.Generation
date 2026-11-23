// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_explicit_mixed_slice_fallbacks : given.mixed_slice_generation
{
    GeneratedScreenplayDefinition _direct = null!;
    GeneratedScreenplayDefinition _snapshot = null!;
    GeneratedScreenplayDefinition _reversed = null!;
    string _original = string.Empty;

    void Because()
    {
        _original = JsonSerializer.Serialize(Write.Concat(Read));
        _direct = Generator.Generate([Contribution(Write), new AdapterContribution { Adapter = OtherAdapter, Facts = Read }], Options);
        _snapshot = GenerateRun(Write, Read);
        _reversed = GenerateRun([.. Write.Reverse()], [.. Read.Reverse()], reverse: true);
    }

    [Fact] void should_compile_and_canonically_reprint() => _snapshot.IsSuccess.ShouldBeTrue();
    [Fact] void should_agree_across_both_entry_paths() => _direct.Source.ShouldEqual(_snapshot.Source);
    [Fact] void should_keep_source_stable_under_reversed_adapter_and_fact_order() => _snapshot.Source.ShouldEqual(_reversed.Source);
    [Fact] void should_keep_the_complete_snapshot_stable() => AdapterRunProjection(_snapshot.AdapterRun).ShouldEqual(AdapterRunProjection(_reversed.AdapterRun));
    [Fact] void should_emit_the_state_change_slice() => _snapshot.Source.Contains("slice StateChange Write", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_emit_the_state_view_slice() => _snapshot.Source.Contains("slice StateView View", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_lower_the_two_effective_placements() => Normalized(_snapshot).Count(record => record.Disposition == GenerationFactDisposition.Lowered).ShouldEqual(2);
    [Fact] void should_retain_original_placements_as_provenance() => RunOf(_snapshot).Facts.Where(record => record.Fact is ArtifactPlacementFact).All(record => record.Disposition == GenerationFactDisposition.ProvenanceOnly).ShouldBeTrue();
    [Fact] void should_not_conflict_successfully_repaired_inputs() => RunOf(_snapshot).Facts.Any(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeFalse();
    [Fact] void should_keep_every_normalization_input_attributable() => Normalized(_snapshot).All(record => LineageOf(record).Inputs.All(id => RunOf(_snapshot).Facts.Any(input => input.Fact.Id == id))).ShouldBeTrue();
    [Fact] void should_explain_both_original_members() => Normalized(_snapshot).All(record => LineageOf(record).Inputs.Length == 4 && LineageOf(record).Evidence.Length == 2).ShouldBeTrue();
    [Fact] void should_leave_admitted_inputs_unchanged() => JsonSerializer.Serialize(Write.Concat(Read)).ShouldEqual(_original);
}
