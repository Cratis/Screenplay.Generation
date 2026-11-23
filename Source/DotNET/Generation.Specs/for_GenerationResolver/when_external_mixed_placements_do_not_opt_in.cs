// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_external_mixed_placements_do_not_opt_in : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, null), Member("Read", GenerationSliceKind.StateView, null));

    [Fact] void should_keep_both_original_names() => Names(graph).ShouldEqual(["Shared", "Shared"]);
    [Fact] void should_not_record_a_normalization() => graph.PlacementNormalizations.ShouldBeEmpty();
    [Fact] void should_not_claim_a_repair() => graph.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.MixedSliceKindsSplit).ShouldBeFalse();
}
