// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_weaker_source_owner_candidate_disagrees : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "CustomerRegistered"), Artifact(Query, "command", "Register"), Candidate(Event, Event, "self"), Candidate(Event, Query, "weak", EvidenceStrength.Heuristic), Production(Query));

    [Fact] void should_rank_before_collapsing_event_owners() => Placement(result).Placement.Slice.ShouldEqual("EventSource");
    [Fact] void should_keep_weaker_production_proof() => result.Facts.Single(record => record.Fact is ArtifactPlacementFact).Lineage!.Inputs.ShouldContain(new FactId { Value = "application:produces" });
}
