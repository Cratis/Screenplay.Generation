// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_a_generated_split_name_collides_with_an_existing_sibling : given.mixed_slice_placements
{
    void Because() => graph = Resolve(Member("Write", GenerationSliceKind.StateChange, "Common"), Member("Read", GenerationSliceKind.StateView, "Common"), Member("Existing", GenerationSliceKind.StateView, null, "CommonStateChange"));

    [Fact] void should_keep_the_exact_two_stage_names() => Names(graph).ShouldEqual(["CommonStateChange", "CommonStateChange", "CommonStateView"]);
    [Fact] void should_report_the_remaining_located_conflict() => graph.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingMixedSliceSplit && diagnostic.Severity == GenerationDiagnosticSeverity.Error && diagnostic.Source is not null);
}
