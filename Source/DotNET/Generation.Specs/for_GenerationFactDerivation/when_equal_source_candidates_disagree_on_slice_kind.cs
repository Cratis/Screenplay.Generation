// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_equal_source_candidates_disagree_on_slice_kind : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "Registered"), Candidate(Event, Event, "self"), Candidate(Event, Event, "other") with { Definition = Candidate(Event, Event, "other").Definition with { SliceKind = GenerationSliceKind.StateView } });

    [Fact] void should_not_break_the_tie_by_order() => result.Facts.ShouldBeEmpty();
    [Fact] void should_report_the_conflict() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingSourceOwnerCandidates);
}
