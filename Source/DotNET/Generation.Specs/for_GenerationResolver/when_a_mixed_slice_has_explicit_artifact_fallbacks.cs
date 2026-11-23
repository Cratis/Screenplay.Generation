// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_mixed_slice_has_explicit_artifact_fallbacks : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Register", GenerationSliceKind.StateChange, "Register"), Member("Overview", GenerationSliceKind.StateView, "Overview"));

    [Fact] void should_use_artifact_names_without_kind_suffixes() => Names(graph).ShouldEqual(["Overview", "Register"]);
    [Fact] void should_preserve_module_and_feature_paths() => graph.Placements.ShouldContain(placement => placement.Variants.Single().Placement.Module == "Accounts" && placement.Variants.Single().Placement.Features.SequenceEqual(["Banking"]));
    [Fact] void should_keep_original_and_effective_placements() => graph.PlacementNormalizations.ShouldContain(record => record.Original.Slice == "Shared" && record.Final.Slice == "Register");
    [Fact] void should_emit_one_located_warning_with_conflict_outcome() => graph.Diagnostics.Count(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit && diagnostic.Severity == GenerationDiagnosticSeverity.Warning && diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict && diagnostic.Source is not null).ShouldEqual(1);
}
