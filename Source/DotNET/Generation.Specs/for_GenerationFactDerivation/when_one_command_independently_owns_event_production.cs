// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_one_command_independently_owns_event_production : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "CustomerRegistered"), Artifact(Query, "command", "Register"), Candidate(Event, Query, "candidate"), Production(Query));

    [Fact] void should_use_the_command_source() => Placement(result).Placement.Slice.ShouldEqual("CommandSource");
    [Fact] void should_retain_independent_production_evidence() => result.Facts.Single(record => record.Fact is ArtifactPlacementFact).Lineage!.Inputs.ShouldContain(new FactId { Value = "application:produces" });
}
