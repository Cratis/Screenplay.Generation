// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection;

public class when_role_kinds_are_independent : given.role_candidates
{
    given.selection_result _result = null!;
    void Because()
    {
        var identifier = Candidate("first:ordinary", "firstId", 1);
        identifier = identifier with { Definition = identifier.Definition with { Role = ArtifactMemberRoleKind.Identifier } };
        _result = given.selection_result.FromContributions(Inputs(identifier, Candidate("first:event-source", "secondId", 1)));
    }
    [Fact] void should_select_each_role_independently() => _result.SelectedRoles.Count.ShouldEqual(2);
    [Fact] void should_not_alias_ordinary_and_event_source_identifiers() => _result.Roles.Single(role => role.Definition.Role == ArtifactMemberRoleKind.EventSourceIdentifier).Definition.Member.Name.ShouldEqual("secondId");
}
