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
/// Defines the framework-neutral kinds of validation rules that can be asserted for a concept.
/// </summary>
public enum ConceptValidationRuleKind
{
    /// <summary>
    /// The concept value must satisfy a named predicate implemented outside the generated document.
    /// </summary>
    NamedPredicate = 0
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

/// <summary>
/// Describes one named attribute applied to a concept.
/// </summary>
public sealed record ConceptAttributeDefinition
{
    /// <summary>
    /// Gets the source-level concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the attribute name without its target-language marker.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional documented reason for the attribute.
    /// </summary>
    public string? Reason { get; init; }
}

/// <summary>
/// Asserts one concept attribute with its source evidence.
/// </summary>
public sealed record ConceptAttributeFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted concept attribute.
    /// </summary>
    public required ConceptAttributeDefinition Definition { get; init; }
}

/// <summary>
/// Describes one validation rule applied to a concept independently of its representation and target syntax.
/// </summary>
public sealed record ConceptValidationRuleDefinition
{
    /// <summary>
    /// Gets the exact source-level concept subject.
    /// </summary>
    public required SubjectId Concept { get; init; }

    /// <summary>
    /// Gets the stable adapter-authored identity used to resolve assertions about this rule.
    /// </summary>
    public required string RuleIdentity { get; init; }

    /// <summary>
    /// Gets the kind of validation rule.
    /// </summary>
    public required ConceptValidationRuleKind Kind { get; init; }

    /// <summary>
    /// Gets the named predicate used as the operand for <see cref="ConceptValidationRuleKind.NamedPredicate"/>.
    /// </summary>
    /// <remarks>
    /// The predicate is required for named-predicate rules. It remains nullable in the transport shape so later rule kinds
    /// can add typed operands without requiring adapters to supply an unrelated string value.
    /// </remarks>
    public string? Predicate { get; init; }

    /// <summary>
    /// Gets the optional message shown when validation fails.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets the optional authored file containing the predicate implementation.
    /// </summary>
    public string? ImplementationFile { get; init; }
}

/// <summary>
/// Asserts one concept validation rule with its source evidence.
/// </summary>
public sealed record ConceptValidationRuleFact : GenerationFact
{
    /// <summary>
    /// Gets the asserted concept validation rule.
    /// </summary>
    public required ConceptValidationRuleDefinition Definition { get; init; }
}
