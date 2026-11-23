// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_mixed_slice_splitting_still_collides : given.mixed_slice_generation
{
    GeneratedScreenplayDefinition _result = null!;

    void Because()
    {
        Write = [.. Write.Select(fact => fact is ArtifactPlacementFact { MixedSliceFallback: { } fallback } placement ? placement with { MixedSliceFallback = fallback with { Slice = "Common" } } : fact)];
        Read = [.. Read.Select(fact => fact is ArtifactPlacementFact { MixedSliceFallback: { } fallback } placement ? placement with { MixedSliceFallback = fallback with { Slice = "Common" } } : fact)];
        var sibling = Artifact(ArtifactKind.ReadModel, "Existing", "CommonStateChange", GenerationSliceKind.StateView, 3);
        _result = GenerateRun([.. Write, .. sibling], Read);
    }

    [Fact] void should_fail_with_an_attributable_remaining_conflict() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMixedSliceSplit && diagnostic.Source is not null && diagnostic.Severity == GenerationDiagnosticSeverity.Error);
    [Fact] void should_not_try_a_third_rename() => _result.Graph.PlacementNormalizations.Single(record => record.Artifact.Kind == ArtifactKind.Command).Final.Slice.ShouldEqual("CommonStateChange");
    [Fact] void should_not_mark_the_colliding_effective_placement_lowered() => Normalized(_result).Single(record => ((ArtifactPlacementFact)record.Fact).Artifact.Kind == ArtifactKind.Command).Disposition.ShouldEqual(GenerationFactDisposition.Conflicted);
}
