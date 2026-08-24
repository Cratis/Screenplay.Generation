// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the framework-neutral shape of an exact specification value.
/// </summary>
public enum SpecificationValueKind
{
    /// <summary>
    /// The adapter could not determine a supported value kind.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// An explicit null value.
    /// </summary>
    Null = 0,

    /// <summary>
    /// A text value.
    /// </summary>
    Text = 1,

    /// <summary>
    /// An invariant numeric value.
    /// </summary>
    Number = 2,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 3,

    /// <summary>
    /// An ordered collection value.
    /// </summary>
    Collection = 4,

    /// <summary>
    /// A composite value with named members.
    /// </summary>
    Composite = 5
}

/// <summary>
/// Identifies one exact value within an ordered specification step.
/// </summary>
public sealed record SpecificationValueKey
{
    /// <summary>
    /// Gets the owning specification step.
    /// </summary>
    public required SpecificationStepKey Step { get; init; }

    /// <summary>
    /// Gets the property, argument, result, member, or collection-index path from the step root.
    /// </summary>
    public IReadOnlyList<string> Path { get; init; } = [];
}

/// <summary>
/// Describes one exact typed specification value independently from its evidence.
/// </summary>
public sealed record SpecificationValueDefinition
{
    /// <summary>
    /// Gets the stable value identity.
    /// </summary>
    public required SpecificationValueKey Key { get; init; }

    /// <summary>
    /// Gets the framework-neutral value shape.
    /// </summary>
    public required SpecificationValueKind Kind { get; init; }

    /// <summary>
    /// Gets the declared value type when source establishes it exactly.
    /// </summary>
    public TypeReferenceDefinition? Type { get; init; }

    /// <summary>
    /// Gets the exact text, invariant number, or lowercase Boolean representation for a scalar value.
    /// </summary>
    public string? Scalar { get; init; }

    /// <summary>
    /// Gets ordered collection items or composite members by their exact child identities.
    /// </summary>
    public IReadOnlyList<SpecificationValueKey> Children { get; init; } = [];
}

/// <summary>
/// Asserts one exact typed specification value with its value-level evidence.
/// </summary>
public sealed record SpecificationValueFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted value definition.
    /// </summary>
    public required SpecificationValueDefinition Definition { get; init; }
}
