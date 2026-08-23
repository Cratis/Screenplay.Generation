// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the semantic role of an artifact discovered in source.
/// </summary>
public enum ArtifactKind
{
    /// <summary>
    /// The adapter could not determine a supported artifact role.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A deployable application host.
    /// </summary>
    ApplicationHost = 0,

    /// <summary>
    /// A domain concept or strongly typed value.
    /// </summary>
    Concept = 1,

    /// <summary>
    /// A composite value type.
    /// </summary>
    CompositeType = 2,

    /// <summary>
    /// An imperative intent.
    /// </summary>
    Command = 3,

    /// <summary>
    /// A persisted fact.
    /// </summary>
    Event = 4,

    /// <summary>
    /// A queryable state shape.
    /// </summary>
    ReadModel = 5,

    /// <summary>
    /// An ordinary persisted document not known to be event-built.
    /// </summary>
    Document = 6,

    /// <summary>
    /// An event-sourced aggregate or stream model.
    /// </summary>
    Aggregate = 7,

    /// <summary>
    /// A query entry point.
    /// </summary>
    Query = 8,

    /// <summary>
    /// A projection that builds state from events.
    /// </summary>
    Projection = 9,

    /// <summary>
    /// A stateful event fold.
    /// </summary>
    Reducer = 10,

    /// <summary>
    /// Behavior triggered by an event, message, or schedule.
    /// </summary>
    Reaction = 11,

    /// <summary>
    /// A framework message whose command/event role is not yet resolved.
    /// </summary>
    Message = 12,

    /// <summary>
    /// A message or endpoint handler.
    /// </summary>
    Handler = 13,

    /// <summary>
    /// An HTTP or transport entry point.
    /// </summary>
    Endpoint = 14,

    /// <summary>
    /// A response returned to a caller.
    /// </summary>
    Response = 15,

    /// <summary>
    /// Stateful process or saga state.
    /// </summary>
    Saga = 16
}

/// <summary>
/// Defines the Screenplay slice type inferred for a behavior.
/// </summary>
public enum GenerationSliceKind
{
    /// <summary>
    /// The adapter could not determine a supported slice role.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A command that changes state.
    /// </summary>
    StateChange = 0,

    /// <summary>
    /// A query or projection that exposes state.
    /// </summary>
    StateView = 1,

    /// <summary>
    /// Behavior reacting to an occurrence.
    /// </summary>
    Automation = 2,

    /// <summary>
    /// Behavior translating external input into facts.
    /// </summary>
    Translate = 3
}

/// <summary>
/// Identifies one artifact role for a source-level subject.
/// </summary>
public sealed record ArtifactKey
{
    /// <summary>
    /// Gets the source-level subject.
    /// </summary>
    public required SubjectId Subject { get; init; }

    /// <summary>
    /// Gets the semantic artifact kind.
    /// </summary>
    public required ArtifactKind Kind { get; init; }
}

/// <summary>
/// Describes where an artifact belongs in a Screenplay module, feature, and slice hierarchy.
/// </summary>
public sealed record ArtifactPlacement
{
    /// <summary>
    /// Gets the module name.
    /// </summary>
    public required string Module { get; init; }

    /// <summary>
    /// Gets the feature path from outermost to innermost.
    /// </summary>
    public IReadOnlyList<string> Features { get; init; } = [];

    /// <summary>
    /// Gets the slice name.
    /// </summary>
    public required string Slice { get; init; }

    /// <summary>
    /// Gets the slice kind.
    /// </summary>
    public required GenerationSliceKind SliceKind { get; init; }
}

/// <summary>
/// Describes a reference to a declared or primitive type.
/// </summary>
public sealed record TypeReferenceDefinition
{
    /// <summary>
    /// Gets the Screenplay type name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the exact source-level type subject when the reference targets a discovered artifact.
    /// </summary>
    public SubjectId? Subject { get; init; }

    /// <summary>
    /// Gets whether the value is a collection.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Gets whether the value is optional.
    /// </summary>
    public bool IsOptional { get; init; }
}

/// <summary>
/// Describes one property of an artifact.
/// </summary>
public sealed record PropertyDefinition
{
    /// <summary>
    /// Gets the property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the property type.
    /// </summary>
    public required TypeReferenceDefinition Type { get; init; }

    /// <summary>
    /// Gets whether the property identifies the event source changed by a command.
    /// </summary>
    public bool IsIdentifier { get; init; }
}

/// <summary>
/// Describes an artifact independently of the evidence that established it.
/// </summary>
public sealed record ArtifactDefinition
{
    /// <summary>
    /// Gets the artifact identity and role.
    /// </summary>
    public required ArtifactKey Key { get; init; }

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

    /// <summary>
    /// Gets the artifact properties in declaration order.
    /// </summary>
    public IReadOnlyList<PropertyDefinition> Properties { get; init; } = [];
}

/// <summary>
/// Asserts one artifact definition with its source evidence.
/// </summary>
public sealed record ArtifactFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted artifact definition.
    /// </summary>
    public required ArtifactDefinition Definition { get; init; }
}

/// <summary>
/// Assigns one artifact role to a Screenplay module, feature, and slice hierarchy.
/// </summary>
public sealed record ArtifactPlacementFact : GenerationFact
{
    /// <summary>
    /// Gets the artifact role being placed.
    /// </summary>
    public required ArtifactKey Artifact { get; init; }

    /// <summary>
    /// Gets the asserted placement.
    /// </summary>
    public required ArtifactPlacement Placement { get; init; }
}
