// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines how a domain concept is represented independently of any target language syntax.
/// </summary>
public enum ConceptRepresentationKind
{
    /// <summary>
    /// The concept wraps one supported primitive value.
    /// </summary>
    Primitive = 0,

    /// <summary>
    /// The concept is an enumeration of named values.
    /// </summary>
    Enumeration = 1
}

/// <summary>
/// Defines framework-neutral primitive representations supported by concept lowering.
/// </summary>
public enum GenerationPrimitiveKind
{
    /// <summary>
    /// A universally unique identifier.
    /// </summary>
    Uuid = 0,

    /// <summary>
    /// A string value.
    /// </summary>
    Text = 1,

    /// <summary>
    /// An integral number.
    /// </summary>
    WholeNumber = 2,

    /// <summary>
    /// A decimal or floating-point number.
    /// </summary>
    Number = 3,

    /// <summary>
    /// A Boolean value.
    /// </summary>
    Boolean = 4,

    /// <summary>
    /// A calendar date without a time.
    /// </summary>
    Date = 5,

    /// <summary>
    /// A date and time value.
    /// </summary>
    DateTime = 6
}

/// <summary>
/// Describes the independently proven representation of a concept.
/// </summary>
public sealed record ConceptRepresentationDefinition
{
    /// <summary>
    /// Gets the source-level concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the representation category.
    /// </summary>
    public required ConceptRepresentationKind Kind { get; init; }

    /// <summary>
    /// Gets the primitive representation when <see cref="Kind"/> is <see cref="ConceptRepresentationKind.Primitive"/>.
    /// </summary>
    public GenerationPrimitiveKind? Primitive { get; init; }

    /// <summary>
    /// Gets the enumeration values in declaration order when <see cref="Kind"/> is <see cref="ConceptRepresentationKind.Enumeration"/>.
    /// </summary>
    public IReadOnlyList<string> EnumerationValues { get; init; } = [];
}

/// <summary>
/// Asserts one concept representation with its source evidence.
/// </summary>
public sealed record ConceptRepresentationFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted concept representation.
    /// </summary>
    public required ConceptRepresentationDefinition Definition { get; init; }
}
