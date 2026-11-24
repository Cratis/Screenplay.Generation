// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_complete_ranks_are_ambiguous : given.role_candidates
{
    void Because() => GenerateBoth(Candidate("first:tie", "firstId", 1), Candidate("second:tie", "secondId", 1, producer: Second));
    [Fact] void should_not_break_the_tie_by_adapter_identity() => Forward.AdapterRun.Derivation!.Facts.Select(record => record.Fact).OfType<ArtifactMemberRoleFact>().ShouldBeEmpty();
    [Fact] void should_report_a_local_conflict() => Forward.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection);
    [Fact] void should_give_both_candidates_a_conflict_disposition() => Forward.AdapterRun.Facts.Where(record => record.Fact is ArtifactMemberRoleSelectionFact).All(record => record.Disposition == GenerationFactDisposition.Conflicted).ShouldBeTrue();
    [Fact] void should_have_identical_diagnostics_and_lineage_in_reverse_order() => AdapterRunProjection(Reverse).ShouldEqual(AdapterRunProjection(Forward));
}
