// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationFactDerivation;

public class when_source_owner_selection_has_no_mapper : given.source_placement_candidates
{
    void Because() => result = Derive(Artifact(Event, "event", "Registered"), Candidate(Event, Event, "self"));

    [Fact] void should_not_invent_a_path() => result.Facts.ShouldBeEmpty();
    [Fact] void should_report_the_missing_boundary() => result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.SourcePlacementResolverUnavailable);
}
