// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines a semantic relationship between source-level subjects.
/// </summary>
public enum RelationshipKind
{
    /// <summary>
    /// The adapter could not determine a supported relationship role.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A handler handles a message or command.
    /// </summary>
    Handles = 0,

    /// <summary>
    /// A command or handler reads state.
    /// </summary>
    Reads = 1,

    /// <summary>
    /// A command or reaction appends an event.
    /// </summary>
    Produces = 2,

    /// <summary>
    /// A projection, reducer, or reaction consumes an event.
    /// </summary>
    Consumes = 3,

    /// <summary>
    /// A projection or reducer builds a state model.
    /// </summary>
    Builds = 4,

    /// <summary>
    /// A query or endpoint returns a model or response.
    /// </summary>
    Returns = 5,

    /// <summary>
    /// A handler cascades a message through the local framework pipeline.
    /// </summary>
    Cascades = 6,

    /// <summary>
    /// A handler explicitly publishes or sends a message.
    /// </summary>
    Publishes = 7,

    /// <summary>
    /// A handler performs a side effect.
    /// </summary>
    SideEffect = 8,

    /// <summary>
    /// A behavior starts a new event stream.
    /// </summary>
    StartsStream = 9,

    /// <summary>
    /// A behavior appends to an existing event stream.
    /// </summary>
    Appends = 10,

    /// <summary>
    /// A behavior stores a document.
    /// </summary>
    Stores = 11,

    /// <summary>
    /// A behavior updates a document.
    /// </summary>
    Updates = 12,

    /// <summary>
    /// A behavior deletes a document.
    /// </summary>
    Deletes = 13
}

/// <summary>
/// Identifies one semantic relationship independently of its evidence.
/// </summary>
public sealed record RelationshipKey
{
    /// <summary>
    /// Gets the relationship kind.
    /// </summary>
    public required RelationshipKind Kind { get; init; }

    /// <summary>
    /// Gets the source subject.
    /// </summary>
    public required SubjectId Source { get; init; }

    /// <summary>
    /// Gets the target subject.
    /// </summary>
    public required SubjectId Target { get; init; }

    /// <summary>
    /// Gets an optional stable discriminator when several relationships of the same kind connect the same subjects.
    /// </summary>
    public string? Discriminator { get; init; }
}

/// <summary>
/// Describes a semantic relationship independently of the evidence establishing it.
/// </summary>
public sealed record RelationshipDefinition
{
    /// <summary>
    /// Gets the relationship identity.
    /// </summary>
    public required RelationshipKey Key { get; init; }

    /// <summary>
    /// Gets the optional source member or expression supplying the relationship value.
    /// </summary>
    public string? SourceMember { get; init; }

    /// <summary>
    /// Gets the optional target member receiving the relationship value.
    /// </summary>
    public string? TargetMember { get; init; }

    /// <summary>
    /// Gets whether the related value is a collection, such as a query returning several models.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Gets whether the related value is optional.
    /// </summary>
    public bool IsOptional { get; init; }
}

/// <summary>
/// Asserts one semantic relationship with its source evidence.
/// </summary>
public sealed record RelationshipFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted relationship definition.
    /// </summary>
    public required RelationshipDefinition Definition { get; init; }
}
