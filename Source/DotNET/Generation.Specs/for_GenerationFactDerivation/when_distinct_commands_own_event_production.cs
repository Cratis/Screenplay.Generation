// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_distinct_commands_own_event_production : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "CustomerRegistered"), Artifact(Query, "command", "Register"), CommandDeclaration(), Candidate(Event, Query, "first"), Candidate(Event, Command, "second"), Production(Query), Production(Command, "other-production"));

    [Fact] void should_use_the_event_source_without_a_self_candidate() => Placement(result).Placement.Slice.ShouldEqual("EventSource");
}
