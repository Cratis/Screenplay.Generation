// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_a_producing_owner_has_no_production_proof : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "CustomerRegistered"), Artifact(Query, "command", "Register"), Candidate(Event, Query, "candidate"));

    [Fact] void should_not_silently_select_self() => result.Facts.Select(record => record.Fact).OfType<ArtifactPlacementFact>().ShouldBeEmpty();
    [Fact] void should_explain_the_missing_proof() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidSourceOwnerCandidate);
}
