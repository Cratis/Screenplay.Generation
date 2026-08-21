// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_duplicate_facts : given.facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(FirstAdapter, Fact("first-event", FirstAdapter)),
        Contribution(SecondAdapter, Fact("second-event", SecondAdapter))
    ]);

    [Fact] void should_resolve_one_artifact() => _result.Artifacts.Count.ShouldEqual(1);
    [Fact] void should_resolve_one_definition() => _result.Artifacts.Single().Variants.Count.ShouldEqual(1);
    [Fact] void should_retain_both_pieces_of_evidence() => _result.Artifacts.Single().Variants.Single().Evidence.Count.ShouldEqual(2);
    [Fact] void should_not_report_a_conflict() => _result.Diagnostics.ShouldBeEmpty();
}
