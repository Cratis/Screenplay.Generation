// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ConceptFactAdmissionValidator
{
    public static void Validate(
        ConceptRepresentationFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateConceptOwner(fact, definition.Concept, $"{path}.Definition.Concept", context);
        context.Enum(
            definition.Kind,
            ConceptRepresentationKind.Unknown,
            $"{path}.Definition.Kind",
            fact.Id,
            fact.Subject);
        if (definition.Primitive is { } configuredPrimitive)
        {
            context.Enum(
                configuredPrimitive,
                GenerationPrimitiveKind.Unknown,
                $"{path}.Definition.Primitive",
                fact.Id,
                fact.Subject);
        }

        switch (definition.Kind)
        {
            case ConceptRepresentationKind.Primitive:
                if (definition.Primitive is null)
                {
                    InvalidOperand(fact, $"{path}.Definition.Primitive", "Primitive concepts require a primitive operand", context);
                }

                if (definition.EnumerationValues.Count > 0)
                {
                    InvalidOperand(fact, $"{path}.Definition.EnumerationValues", "Primitive concepts cannot declare enumeration values", context);
                }
                break;
            case ConceptRepresentationKind.Enumeration:
                if (definition.Primitive is not null)
                {
                    InvalidOperand(fact, $"{path}.Definition.Primitive", "Enumeration concepts cannot declare a primitive operand", context);
                }

                if (definition.EnumerationValues.Count == 0)
                {
                    InvalidOperand(fact, $"{path}.Definition.EnumerationValues", "Enumeration concepts require at least one named value", context);
                }
                break;
        }

        for (var index = 0; index < definition.EnumerationValues.Count; index++)
        {
            AdapterContributionAdmissionValidator.ValidateRequiredText(
                definition.EnumerationValues[index],
                $"{path}.Definition.EnumerationValues[{index}]",
                fact.Id,
                fact.Subject,
                context);
        }

        foreach (var duplicate in definition.EnumerationValues
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .GroupBy(value => value, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1))
        {
            InvalidOperand(
                fact,
                $"{path}.Definition.EnumerationValues",
                $"Enumeration value '{duplicate.Key}' occurs more than once",
                context);
        }
    }

    public static void Validate(
        ConceptAttributeFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateConceptOwner(fact, definition.Concept, $"{path}.Definition.Concept", context);
        context.Enum(
            definition.Kind,
            ConceptAttributeKind.Unknown,
            $"{path}.Definition.Kind",
            fact.Id,
            fact.Subject);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            definition.Name,
            $"{path}.Definition.Name",
            fact.Id,
            fact.Subject,
            context);
    }

    public static void Validate(
        ConceptValidationRuleFact fact,
        string path,
        AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        ValidateConceptOwner(fact, definition.Concept, $"{path}.Definition.Concept", context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(
            definition.RuleIdentity,
            $"{path}.Definition.RuleIdentity",
            fact.Id,
            fact.Subject,
            context);
        context.Enum(
            definition.Kind,
            ConceptValidationRuleKind.Unknown,
            $"{path}.Definition.Kind",
            fact.Id,
            fact.Subject);

        if (definition.Kind == ConceptValidationRuleKind.NamedPredicate)
        {
            AdapterContributionAdmissionValidator.ValidateRequiredText(
                definition.Predicate,
                $"{path}.Definition.Predicate",
                fact.Id,
                fact.Subject,
                context);
        }
    }

    static void ValidateConceptOwner(
        GenerationFact fact,
        SubjectId concept,
        string path,
        AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateSubject(concept, path, fact.Id, context);
        if (concept != fact.Subject)
        {
            context.Add(
                AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                path,
                $"Concept owner '{concept.Value}' does not equal fact subject '{fact.Subject.Value}'",
                fact.Id,
                fact.Subject);
        }
    }

    static void InvalidOperand(
        GenerationFact fact,
        string path,
        string message,
        AdapterContributionAdmissionContext context) =>
        context.Add(
            AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand,
            path,
            message,
            fact.Id,
            fact.Subject);
}
