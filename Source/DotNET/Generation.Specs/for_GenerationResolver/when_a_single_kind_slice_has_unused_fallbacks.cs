// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_single_kind_slice_has_unused_fallbacks : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, "Different"));

    [Fact] void should_keep_the_original_slice() => Names(graph).ShouldEqual(["Shared"]);
    [Fact] void should_not_normalize_or_warn() => graph.PlacementNormalizations.ShouldBeEmpty();
}
