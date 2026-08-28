// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines one node in the exact optionality and collection shape of a type use.
/// </summary>
public enum TypeUseShapeKind
{
    /// <summary>
    /// The adapter could not determine a supported type-use shape.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The terminal named type.
    /// </summary>
    Named = 0,

    /// <summary>
    /// An optional wrapper around the following shape node.
    /// </summary>
    Optional = 1,

    /// <summary>
    /// A collection wrapper around the following shape node.
    /// </summary>
    Collection = 2
}

/// <summary>
/// Describes one exact source type use independently from any artifact binding.
/// </summary>
/// <remarks>
/// Shape nodes are ordered from the outermost wrapper to the terminal <see cref="TypeUseShapeKind.Named"/> node.
/// </remarks>
public sealed record TypeUseDefinition
{
    /// <summary>
    /// Gets the source type name observed at the use site.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the exact source-level type subject observed at the use site, when available.
    /// </summary>
    public SubjectId? ObservedTypeSubject { get; init; }

    /// <summary>
    /// Gets the exact optionality and collection shape from outermost wrapper to named type.
    /// </summary>
    public IReadOnlyList<TypeUseShapeKind> Shape { get; init; } = [TypeUseShapeKind.Named];
}

/// <summary>
/// Describes the exact type use of one artifact member.
/// </summary>
public sealed record ArtifactMemberTypeUseDefinition
{
    /// <summary>
    /// Gets the exact artifact member using the type.
    /// </summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>
    /// Gets the observed source type use.
    /// </summary>
    public required TypeUseDefinition Type { get; init; }
}

/// <summary>
/// Asserts the exact source type use of one artifact member.
/// </summary>
public sealed record ArtifactMemberTypeUseFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted member type use.
    /// </summary>
    public required ArtifactMemberTypeUseDefinition Definition { get; init; }
}

/// <summary>
/// Describes an exact binding from one member type use to a declared artifact role.
/// </summary>
public sealed record TypeUseBindingDefinition
{
    /// <summary>
    /// Gets the exact artifact member whose type use is bound.
    /// </summary>
    public required ArtifactMemberKey Member { get; init; }

    /// <summary>
    /// Gets the exact artifact role targeted by the type use.
    /// </summary>
    public required ArtifactKey Target { get; init; }
}

/// <summary>
/// Asserts an exact binding from one member type use to a declared artifact role.
/// </summary>
public sealed record TypeUseBindingFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted type-use binding.
    /// </summary>
    public required TypeUseBindingDefinition Definition { get; init; }
}
