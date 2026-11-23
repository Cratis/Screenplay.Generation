// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_mixed_group_contains_an_unknown_kind : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, "Write"), Member("Unknown", GenerationSliceKind.Unknown, "Unknown"));

    [Fact] void should_not_invent_a_semantic_kind() => Names(graph).ShouldEqual(["Shared"]);
    [Fact] void should_preserve_the_unknown_kind_diagnostic() => graph.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.UnsupportedSliceKind && diagnostic.Outcome == GenerationDiagnosticOutcome.Unknown);
    [Fact] void should_not_apply_a_split() => graph.PlacementNormalizations.ShouldBeEmpty();
}
