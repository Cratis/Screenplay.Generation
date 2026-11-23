// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_opted_in_members_have_no_artifact_fallback_names : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, null, splitWithoutName: true), Member("Read", GenerationSliceKind.StateView, null, splitWithoutName: true));

    [Fact] void should_use_the_original_name_as_the_suffix_base() => Names(graph).ShouldEqual(["SharedStateChange", "SharedStateView"]);
    [Fact] void should_retain_the_first_stage_name() => graph.PlacementNormalizations.All(record => record.AfterArtifact.Slice == "Shared").ShouldBeTrue();
}
