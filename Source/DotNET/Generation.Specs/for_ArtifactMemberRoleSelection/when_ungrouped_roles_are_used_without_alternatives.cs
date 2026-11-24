// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_ungrouped_roles_are_used_without_alternatives : given.role_candidates
{
    void Because() => GenerateBoth(new ArtifactMemberRoleFact { Id = new() { Value = "first:direct" }, Subject = Subject, Evidence = Proof, Definition = new() { Member = new() { Artifact = Artifact, Name = "firstId" }, Role = ArtifactMemberRoleKind.EventSourceIdentifier } });
    [Fact] void should_keep_legacy_identifier_semantics() => Forward.Source.ShouldContain("firstId Uuid identifier");
    [Fact] void should_not_introduce_a_selection_rule() => Forward.AdapterRun.Derivation!.Rules.Select(rule => rule.Rule.Id).ShouldNotContain(given.selection_result.Rule.Id);
    [Fact] void should_not_introduce_selection_diagnostics() => Forward.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(GenerationDiagnosticCodes.ConflictingArtifactMemberRoleSelection);
}
