// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_yielding_concepts_arrive_in_different_orders : given.yielding_concepts
{
    GeneratedScreenplayDefinition _forward = null!;
    GeneratedScreenplayDefinition _reverse = null!;
    GeneratedScreenplayDefinition _direct = null!;

    void Because()
    {
        var other = new AdapterIdentity { Id = "other", Version = "1.0.0" };
        var first = Completed(Adapter, [.. Command(), .. Candidate("enum", true)]);
        var second = Completed(other, Candidate("vogen", false, true, other));
        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _forward = Generator.Generate(Snapshot(first, second), options);
        _reverse = Generator.Generate(Snapshot(second, first), options);
        _direct = Generator.Generate([Contribution([.. Command(), .. Candidate("enum", true)]), new AdapterContribution { Adapter = other, Facts = Candidate("vogen", false, true, other) }], options);
    }

    [Fact] void should_succeed() => _forward.IsSuccess.ShouldBeTrue();
    [Fact] void should_keep_identical_source() => _reverse.Source.ShouldEqual(_forward.Source);
    [Fact] void should_keep_identical_diagnostics() => AdapterRunProjection(_reverse.Diagnostics).ShouldEqual(AdapterRunProjection(_forward.Diagnostics));
    [Fact] void should_keep_identical_fact_dispositions() => AdapterRunProjection(_reverse.AdapterRun.Facts).ShouldEqual(AdapterRunProjection(_forward.AdapterRun.Facts));
    [Fact] void should_keep_identical_lineage() => AdapterRunProjection(_reverse.AdapterRun!.Derivation).ShouldEqual(AdapterRunProjection(_forward.AdapterRun!.Derivation));
    [Fact] void should_apply_selection_to_direct_contributions() => _direct.Source.ShouldEqual(_forward.Source);
}
