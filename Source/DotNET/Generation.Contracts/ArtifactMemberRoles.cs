// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines a semantic role established for one artifact member.
/// </summary>
public enum ArtifactMemberRoleKind
{
    /// <summary>
    /// The adapter could not determine a supported member role.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A typed identifier for the owning artifact.
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// The identifier of the event source targeted by the owning behavior.
    /// </summary>
    EventSourceIdentifier = 1
}

/// <summary>
/// Describes one semantic role established for an exact artifact member.
/// </summary>
public sealed record ArtifactMemberRoleDefinition
{
    /// <summary>
    /// Gets the exact artifact member.
    /// </summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>
    /// Gets the semantic member role.
    /// </summary>
    public required ArtifactMemberRoleKind Role { get; init; }
}

/// <summary>
/// Asserts one semantic role for an exact artifact member.
/// </summary>
public sealed record ArtifactMemberRoleFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted member role.
    /// </summary>
    public required ArtifactMemberRoleDefinition Definition { get; init; }
}
