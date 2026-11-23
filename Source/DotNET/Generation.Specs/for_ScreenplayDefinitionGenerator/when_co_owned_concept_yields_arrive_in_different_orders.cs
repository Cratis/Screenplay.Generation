// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_co_owned_concept_yields_arrive_in_different_orders : given.co_owned_yielding_concepts
{
    GeneratedScreenplayDefinition _twoForward = null!;
    GeneratedScreenplayDefinition _twoReverse = null!;
    GeneratedScreenplayDefinition _threeForward = null!;
    GeneratedScreenplayDefinition _threeReverse = null!;
    GeneratedScreenplayDefinition _direct = null!;

    void Because()
    {
        var first = CoOwner("first");
        var second = CoOwner("second");
        var third = CoOwner("third");
        _twoForward = GenerateCoOwners(first, second);
        _twoReverse = GenerateCoOwners([.. second.Reverse()], [.. first.Reverse()]);
        _threeForward = GenerateCoOwners(first, second, third);
        _threeReverse = GenerateCoOwners([.. third.Reverse()], [.. second.Reverse()], [.. first.Reverse()]);
        _direct = Generator.Generate(
        [
            new AdapterContribution { Adapter = third[0].Evidence.Adapter, Facts = [.. third.Reverse()] },
            new AdapterContribution { Adapter = second[0].Evidence.Adapter, Facts = [.. second.Reverse()] },
            new AdapterContribution { Adapter = first[0].Evidence.Adapter, Facts = [.. first.Reverse()] },
            Contribution([.. Command(), .. Candidate("vogen", false, true)])
        ],
        new ScreenplayGenerationOptions { Domain = "Ordering" });
    }

    [Fact] void should_keep_identical_source_for_two_co_owners() => _twoReverse.Source.ShouldEqual(_twoForward.Source);
    [Fact] void should_keep_identical_diagnostics_for_two_co_owners() => AdapterRunProjection(_twoReverse.Diagnostics).ShouldEqual(AdapterRunProjection(_twoForward.Diagnostics));
    [Fact] void should_keep_identical_dispositions_and_lineage_for_two_co_owners() => AdapterRunProjection(_twoReverse.AdapterRun).ShouldEqual(AdapterRunProjection(_twoForward.AdapterRun));
    [Fact] void should_keep_identical_source_for_three_co_owners() => _threeReverse.Source.ShouldEqual(_threeForward.Source);
    [Fact] void should_keep_identical_diagnostics_for_three_co_owners() => AdapterRunProjection(_threeReverse.Diagnostics).ShouldEqual(AdapterRunProjection(_threeForward.Diagnostics));
    [Fact] void should_keep_identical_dispositions_and_lineage_for_three_co_owners() => AdapterRunProjection(_threeReverse.AdapterRun).ShouldEqual(AdapterRunProjection(_threeForward.AdapterRun));
    [Fact] void should_apply_coalescing_to_direct_contributions() => _direct.Source.ShouldEqual(_threeForward.Source);
    [Fact] void should_keep_identical_yield_diagnostics_for_direct_contributions() => AdapterRunProjection(_direct.Diagnostics.Where(IsWarning)).ShouldEqual(AdapterRunProjection(_threeForward.Diagnostics.Where(IsWarning)));
}
