// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_source_owner_candidates_arrive_in_different_orders : given.source_placement_candidates
{
    GenerationDerivationSnapshot reversed = null!;
    void Because()
    {
        GenerationFact[] facts = [Artifact(Event, "event", "CustomerRegistered"), Artifact(Query, "command", "Register"), Candidate(Event, Query, "first"), Candidate(Event, Query, "repeat"), Production(Query)];
        result = Resolve(facts);
        reversed = Resolve([.. facts.Reverse()]);
    }

    [Fact] void should_preserve_placement_and_lineage() => Projection(result).ShouldEqual(Projection(reversed));
    [Fact] void should_treat_repeated_owners_as_one_producer() => Placement(result).Placement.Slice.ShouldEqual("CommandSource");
}
