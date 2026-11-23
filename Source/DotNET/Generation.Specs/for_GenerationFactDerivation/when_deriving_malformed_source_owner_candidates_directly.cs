// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_deriving_malformed_source_owner_candidates_directly : given.source_placement_candidates
{
    void Because() => result = Resolve(Artifact(Event, "event", "Registered"), Candidate(Event, Event, "self") with { Definition = null! });

    [Fact] void should_report_invalid_candidate_instead_of_throwing() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidSourceOwnerCandidate);
}
