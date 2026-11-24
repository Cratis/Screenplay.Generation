// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_legacy_assertions_are_mixed_with_grouped_selection : given.role_candidates
{
    given.selection_result _ungrouped = null!;
    given.selection_result _aggregate = null!;
    void Because()
    {
        var candidate = Candidate("first:configured", "secondId", 1);
        var role = new ArtifactMemberRoleFact { Id = new() { Value = "first:legacy" }, Subject = Subject, Evidence = Proof, Definition = new() { Member = new() { Artifact = Artifact, Name = "firstId" }, Role = ArtifactMemberRoleKind.EventSourceIdentifier } };
        _ungrouped = given.selection_result.FromContributions(Inputs(candidate, role));
        var declaration = Declarations().OfType<ArtifactFact>().First();
        var aggregate = declaration with { Id = new() { Value = "first:aggregate-identifier" }, Definition = declaration.Definition with { Properties = [Property("firstId", "Uuid") with { IsIdentifier = true }] } };
        _aggregate = given.selection_result.FromContributions(Inputs(candidate, aggregate));
    }
    [Fact] void should_reject_a_direct_role_assertion_in_the_group() => _ungrouped.SelectedRoles.ShouldBeEmpty();
    [Fact] void should_report_the_direct_role_conflict() => _ungrouped.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection);
    [Fact] void should_reject_an_aggregate_identifier_in_the_group() => _aggregate.SelectedRoles.ShouldBeEmpty();
    [Fact] void should_report_the_aggregate_conflict() => _aggregate.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection);
}
