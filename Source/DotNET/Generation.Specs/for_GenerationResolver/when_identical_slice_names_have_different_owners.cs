// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_identical_slice_names_have_different_owners : given.mixed_slice_placements
{
    void Because() => graph = Resolve(
        Member("Write", GenerationSliceKind.StateChange, "Write"),
        Member("OtherModule", GenerationSliceKind.StateView, "Read", module: "Other"),
        Member("OtherFeature", GenerationSliceKind.StateView, "Read", features: ["Other"]));

    [Fact] void should_not_group_unrelated_module_or_feature_owners() => graph.PlacementNormalizations.ShouldBeEmpty();
    [Fact] void should_keep_the_source_derived_names() => Names(graph).ShouldEqual(["Shared", "Shared", "Shared"]);
}
