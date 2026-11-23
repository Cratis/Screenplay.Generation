// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_a_yielding_concept_conflicts_with_an_unbound_type_name : given.yielding_concepts
{
    GeneratedScreenplayDefinition _forward = null!;
    GeneratedScreenplayDefinition _reverse = null!;

    void Because()
    {
        var other = new AdapterIdentity { Id = "other", Version = "1.0.0" };
        var first = Completed(Adapter, [.. Command(), .. Candidate("enum", true)]);
        var second = Completed(other, UnboundMember("Permissions", other));
        var options = new ScreenplayGenerationOptions { Domain = "Ordering" };
        _forward = Generator.Generate(Snapshot(first, second), options);
        _reverse = Generator.Generate(Snapshot(second, first), options);
    }

    [Fact] void should_succeed() => _forward.IsSuccess.ShouldBeTrue();
    [Fact] void should_drop_the_yielding_concept() => _forward.Source.ShouldNotContain("concept Permissions");
    [Fact] void should_report_the_adapter_diagnostic_exactly_once() => _forward.Diagnostics.Count(IsWarning).ShouldEqual(1);
    [Fact] void should_keep_its_own_member_unbound() => _forward.Source.ShouldContain("permissions Int");
    [Fact] void should_keep_the_reserved_type_name_unbound() => _forward.Source.ShouldContain("unbound Permissions");
    [Fact] void should_keep_identical_source_regardless_of_contribution_order() => _reverse.Source.ShouldEqual(_forward.Source);
    [Fact] void should_keep_identical_diagnostics_regardless_of_contribution_order() => AdapterRunProjection(_reverse.Diagnostics).ShouldEqual(AdapterRunProjection(_forward.Diagnostics));
    [Fact] void should_keep_identical_lineage_regardless_of_contribution_order() => AdapterRunProjection(_reverse.AdapterRun!.Derivation).ShouldEqual(AdapterRunProjection(_forward.AdapterRun!.Derivation));
}
