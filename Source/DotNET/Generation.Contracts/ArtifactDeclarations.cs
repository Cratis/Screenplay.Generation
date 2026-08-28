// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Identifies one declared member of an exact artifact role.
/// </summary>
public sealed record ArtifactMemberKey
{
    /// <summary>
    /// Gets the artifact role that owns the member.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets the member name within the artifact declaration.
    /// </summary>
    public required string Name { get; init; }
}

/// <summary>
/// Describes an artifact declaration without repeating its member declarations.
/// </summary>
public sealed record ArtifactDeclarationDefinition
{
    /// <summary>
    /// Gets the artifact identity and role.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets the display and Screenplay declaration name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional human-readable description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the repository-relative source file realizing the artifact.
    /// </summary>
    public string? File { get; init; }
}

/// <summary>
/// Asserts one artifact declaration independently from its members.
/// </summary>
public sealed record ArtifactDeclarationFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted artifact declaration.
    /// </summary>
    public required ArtifactDeclarationDefinition Definition { get; init; }
}

/// <summary>
/// Describes one declared artifact member and its position in the authored declaration.
/// </summary>
public sealed record ArtifactMemberDeclarationDefinition
{
    /// <summary>
    /// Gets the exact artifact member.
    /// </summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>
    /// Gets the zero-based member position in the authored declaration.
    /// </summary>
    public required int DeclarationOrder { get; init; }
}

/// <summary>
/// Asserts one artifact member declaration without repeating the complete artifact.
/// </summary>
public sealed record ArtifactMemberDeclarationFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted member declaration.
    /// </summary>
    public required ArtifactMemberDeclarationDefinition Definition { get; init; }
}
