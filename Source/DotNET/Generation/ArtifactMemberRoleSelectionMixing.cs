// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionMixing
{
    public static IReadOnlyList<GenerationFact> Assertions(ArtifactMemberRoleSelectionFact candidate, IEnumerable<GenerationFact> facts) =>
        candidate.Definition is not { Artifact: { } artifact } definition ? [] : [.. facts.Where(fact => fact switch
        {
            ArtifactMemberRoleFact role => role.Definition.Member.Artifact == artifact && role.Definition.Role == definition.Role,
            ArtifactFact aggregate => definition.Role == ArtifactMemberRoleKind.EventSourceIdentifier && aggregate.Definition.Key == artifact && aggregate.Definition.Properties.Any(property => property.IsIdentifier),
            _ => false
        })];
}
