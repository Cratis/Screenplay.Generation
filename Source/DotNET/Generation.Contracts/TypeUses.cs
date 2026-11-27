// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

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
/// Defines the source construct a collection step of a type use was observed from.
/// </summary>
public enum TypeUseCollectionOriginKind
{
    /// <summary>
    /// The origin is unknown and never proves a wrapper.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The collection step is a source array.
    /// </summary>
    Array = 1,

    /// <summary>
    /// The collection step is a named enumerable that is not an array.
    /// </summary>
    Enumerable = 2
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

    /// <summary>Gets the exact observed target scope; a non-null scope requires an observed subject.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackendScopeKey? ObservedScope { get; init; }

    /// <summary>
    /// Gets the exact optionality and collection shape from outermost wrapper to named type.
    /// </summary>
    public IReadOnlyList<TypeUseShapeKind> Shape { get; init; } = [TypeUseShapeKind.Named];

    /// <summary>Gets optional source identities aligned with every node of <see cref="Shape"/>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TypeUseNodeObservation>? Nodes { get; init; }
}

/// <summary>Describes the exact source symbol at one shape node, before peeling it.</summary>
public sealed record TypeUseNodeObservation
{
    /// <summary>Gets the source symbol's printable name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the exact source symbol subject, when owned by the analyzed source.</summary>
    public SubjectId? Subject { get; init; }

    /// <summary>Gets the exact source symbol scope; a scope requires a subject.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public BackendScopeKey? Scope { get; init; }
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

    /// <summary>
    /// Gets whether the producer requests the legacy printable view of a jagged array member.
    /// </summary>
    /// <remarks>
    /// The request is never evidence of array identity; it is honored only together with matching <see cref="CollectionOrigins"/>.
    /// </remarks>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool UseLegacyJaggedArrayReference { get; init; }

    /// <summary>
    /// Gets the producer-observed origin of every <see cref="TypeUseShapeKind.Collection"/> step of <see cref="Type"/>, outermost first.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<TypeUseCollectionOriginKind>? CollectionOrigins { get; init; }
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
