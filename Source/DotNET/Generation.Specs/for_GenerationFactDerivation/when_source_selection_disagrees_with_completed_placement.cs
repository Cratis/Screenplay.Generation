// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_source_selection_disagrees_with_completed_placement : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "Registered"), Candidate(Event, Event, "self"), new ArtifactPlacementFact { Id = new() { Value = "application:placement" }, Subject = Event.Subject, Evidence = UsageEvidence(), Artifact = Event, Placement = new() { Module = "Other", Slice = "Different", SliceKind = GenerationSliceKind.StateChange } });

    [Fact] void should_not_override_completed_evidence() => result.Facts.ShouldBeEmpty();
    [Fact] void should_report_the_conflict() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingSourceOwnerCandidates);
}
