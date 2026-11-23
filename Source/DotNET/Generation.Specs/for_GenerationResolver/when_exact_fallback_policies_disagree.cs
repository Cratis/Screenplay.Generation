// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_exact_fallback_policies_disagree : given.mixed_slice_placements
{
    void Because()
    {
        var write = Member("Write", GenerationSliceKind.StateChange, "Write");
        var placement = (ArtifactPlacementFact)write[1];
        var conflicting = placement with { Id = new() { Value = "first:conflicting" }, MixedSliceFallback = new() { Slice = "Different", Evidence = placement.Evidence } };
        graph = Resolve([.. write, conflicting], Member("Read", GenerationSliceKind.StateView, null));
    }

    [Fact] void should_preserve_the_exact_policy_conflict() => graph.Placements.Single(placement => placement.Artifact.Kind == ArtifactKind.Command).IsConflicted.ShouldBeTrue();
    [Fact] void should_not_silently_choose_a_fallback() => graph.PlacementNormalizations.ShouldBeEmpty();
    [Fact] void should_report_the_placement_conflict() => graph.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingPlacement);
}
