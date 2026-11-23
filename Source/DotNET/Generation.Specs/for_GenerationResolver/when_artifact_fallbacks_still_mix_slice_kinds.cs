// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_artifact_fallbacks_still_mix_slice_kinds : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, "Common"), Member("Read", GenerationSliceKind.StateView, "Common"), Member("React", GenerationSliceKind.Automation, "Common"), Member("Translate", GenerationSliceKind.Translate, "Common"));

    [Fact] void should_use_all_four_known_kind_tokens() => Names(graph).ShouldEqual(["CommonAutomation", "CommonStateChange", "CommonStateView", "CommonTranslate"]);
    [Fact] void should_retain_both_rename_stages() => graph.PlacementNormalizations.All(record => record.AfterArtifact.Slice == "Common" && record.Final.Slice != "Common").ShouldBeTrue();
}
