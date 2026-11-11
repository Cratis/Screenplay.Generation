// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using static Cratis.Screenplay.Generation.LoweringSupport;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Lowers the concepts of a resolved graph, with their attributes and validation rules.
/// </summary>
internal static class ConceptLowering
{
    static readonly Dictionary<GenerationPrimitiveKind, string> _primitiveTypes = new()
    {
        [GenerationPrimitiveKind.Uuid] = "Uuid",
        [GenerationPrimitiveKind.Text] = "String",
        [GenerationPrimitiveKind.WholeNumber] = "Int",
        [GenerationPrimitiveKind.Number] = "Decimal",
        [GenerationPrimitiveKind.Boolean] = "Bool",
        [GenerationPrimitiveKind.Date] = "Date",
        [GenerationPrimitiveKind.DateTime] = "DateTime"
    };

    public static (ConceptSyntax[] Concepts, IReadOnlyDictionary<string, string> Names) Lower(
        ResolvedApplicationGraph graph,
        IReadOnlyList<ArtifactDefinition> definitions,
        LoweringSink sink)
    {
        var conceptDefinitions = definitions.Where(_ => _.Key.Kind == ArtifactKind.Concept).ToArray();
        var conflictingNames = ReportNameConflicts(conceptDefinitions, sink);
        var concepts = new List<ConceptSyntax>();
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in conceptDefinitions
                     .Where(_ => !conflictingNames.Contains(_.Name))
                     .OrderBy(_ => _.Name, StringComparer.Ordinal)
                     .ThenBy(_ => _.Key.Subject.Value, StringComparer.Ordinal))
        {
            if (TryLower(graph, definition, sink) is { } concept)
            {
                concepts.Add(concept);
                names[definition.Key.Subject.Value] = definition.Name;
            }
        }

        return ([.. concepts], names);
    }

    static HashSet<string> ReportNameConflicts(IReadOnlyList<ArtifactDefinition> conceptDefinitions, LoweringSink sink)
    {
        var conflicts = conceptDefinitions
            .GroupBy(_ => _.Name, StringComparer.Ordinal)
            .Where(_ => _.Select(definition => definition.Key.Subject.Value).Distinct(StringComparer.Ordinal).Count() > 1)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .ToArray();
        foreach (var conflict in conflicts)
        {
            var definitions = conflict.ToArray();
            var diagnostic = new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.ConflictingConceptName,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Conflict,
                Message = $"Concept name '{conflict.Key}' is required by {definitions.Length} distinct source subjects",
                Subject = definitions.OrderBy(_ => _.Key.Subject.Value, StringComparer.Ordinal).First().Key.Subject
            };
            sink.Diagnostics.Add(diagnostic);
            foreach (var definition in definitions)
            {
                sink.Coverage.Conflicted(GenerationFactSemanticKey.Artifact(definition), diagnostic);
            }
        }

        return [.. conflicts.Select(_ => _.Key)];
    }

    static ConceptSyntax? TryLower(ResolvedApplicationGraph graph, ArtifactDefinition definition, LoweringSink sink)
    {
        var resolved = graph.ConceptRepresentations.FirstOrDefault(_ => _.Concept == definition.Key.Subject);
        if (resolved is null)
        {
            sink.Omit(
                Attach(
                    Warning(
                        GenerationDiagnosticCodes.MissingConceptRepresentation,
                        GenerationDiagnosticOutcome.Unknown,
                        $"Concept '{definition.Name}' has no proven representation and was omitted"),
                    definition,
                    []),
                GenerationFactSemanticKey.Artifact(definition));
            return null;
        }

        if (resolved.IsConflicted)
        {
            return null;
        }

        var variant = resolved.Variants.Single();
        var representation = variant.Definition;
        var check = Check(definition, representation, variant.Evidence);
        if (check.Problem is { } problem)
        {
            sink.Omit(
                problem,
                GenerationFactSemanticKey.Artifact(definition),
                GenerationFactSemanticKey.ConceptRepresentation(representation));
            return null;
        }

        var concept = new ConceptSyntax(
            definition.Name,
            check.Type!,
            BuildAttributes(graph, definition, sink),
            check.Values,
            Generated,
            BuildValidations(graph, definition, sink))
        {
            File = FileFrom(definition.File)
        };
        sink.Coverage.Lowered(GenerationFactSemanticKey.Artifact(definition));
        sink.Coverage.Lowered(GenerationFactSemanticKey.ConceptRepresentation(representation));
        return concept;
    }

    static ConceptCheck Check(ArtifactDefinition definition, ConceptRepresentationDefinition representation, IEnumerable<Evidence> evidence)
    {
        var problem = KindProblem(definition, representation, evidence) ?? PrimitiveProblem(definition, representation, evidence);
        if (problem is not null)
        {
            return new(null, [], problem);
        }

        if (TypeOf(representation) is not { } type)
        {
            return new(
                null,
                [],
                Attach(
                    Warning(
                        GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                        GenerationDiagnosticOutcome.Unsupported,
                        $"Concept '{definition.Name}' has an invalid or unsupported {representation.Kind} representation and was omitted"),
                    definition,
                    evidence));
        }

        IReadOnlyList<string> values = representation.Kind == ConceptRepresentationKind.Enumeration
            ? [.. representation.EnumerationValues.Select(EnumValue)]
            : [];
        return HasValidEnumerationValues(values)
            ? new(type, values, null)
            : new(
                null,
                [],
                Attach(
                    Warning(
                        GenerationDiagnosticCodes.UnsupportedConceptRepresentation,
                        GenerationDiagnosticOutcome.Unsupported,
                        $"Concept '{definition.Name}' has empty or duplicate enumeration values after Screenplay naming and was omitted"),
                    definition,
                    evidence));
    }

    static GenerationDiagnostic? KindProblem(ArtifactDefinition definition, ConceptRepresentationDefinition representation, IEnumerable<Evidence> evidence) =>
        representation.Kind == ConceptRepresentationKind.Unknown || !Enum.IsDefined(representation.Kind)
            ? Attach(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedConceptRepresentationKind,
                    OutcomeFor(representation.Kind, ConceptRepresentationKind.Unknown),
                    $"Concept '{definition.Name}' uses unknown or undefined ConceptRepresentationKind value '{(int)representation.Kind}' and was omitted"),
                definition,
                evidence)
            : null;

    static GenerationDiagnostic? PrimitiveProblem(ArtifactDefinition definition, ConceptRepresentationDefinition representation, IEnumerable<Evidence> evidence) =>
        representation.Primitive is { } primitive && (primitive == GenerationPrimitiveKind.Unknown || !Enum.IsDefined(primitive))
            ? Attach(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedPrimitiveKind,
                    OutcomeFor(primitive, GenerationPrimitiveKind.Unknown),
                    $"Concept '{definition.Name}' uses unknown or undefined GenerationPrimitiveKind value '{(int)primitive}' and was omitted"),
                definition,
                evidence)
            : null;

    static ConceptAttributeSyntax[] BuildAttributes(ResolvedApplicationGraph graph, ArtifactDefinition concept, LoweringSink sink)
    {
        var attributes = new List<ConceptAttributeSyntax>();
        foreach (var resolved in graph.ConceptAttributes
                     .Where(_ => _.Concept == concept.Key.Subject && !_.IsConflicted)
                     .OrderBy(_ => _.Name, StringComparer.Ordinal))
        {
            var variant = resolved.Variants.Single();
            var definition = variant.Definition;
            var problem = AttributeProblem(concept, definition, variant.Evidence);
            if (problem is not null)
            {
                sink.Omit(problem, GenerationFactSemanticKey.ConceptAttribute(definition));
                continue;
            }

            attributes.Add(new ConceptAttributeSyntax(definition.Name, Generated, definition.Reason));
            sink.Coverage.Lowered(GenerationFactSemanticKey.ConceptAttribute(definition));
        }

        return [.. attributes];
    }

    static GenerationDiagnostic? AttributeProblem(ArtifactDefinition concept, ConceptAttributeDefinition definition, IEnumerable<Evidence> evidence)
    {
        if (definition.Kind != ConceptAttributeKind.Named)
        {
            return Attach(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedConceptAttributeKind,
                    OutcomeFor(definition.Kind, ConceptAttributeKind.Unknown),
                    $"Concept '{concept.Name}' has unknown or undefined ConceptAttributeKind value '{(int)definition.Kind}', which was omitted"),
                concept,
                evidence);
        }

        return IsIdentifier(definition.Name)
            ? null
            : Attach(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedConceptAttribute,
                    GenerationDiagnosticOutcome.Unsupported,
                    $"Concept '{concept.Name}' has invalid attribute name '{definition.Name}', which was omitted"),
                concept,
                evidence);
    }

    static ValidateSyntax[] BuildValidations(ResolvedApplicationGraph graph, ArtifactDefinition concept, LoweringSink sink)
    {
        var rules = new List<ValidationRuleSyntax>();
        foreach (var resolved in graph.ConceptValidationRules
                     .Where(_ => _.Concept == concept.Key.Subject)
                     .OrderBy(_ => _.RuleIdentity, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(resolved.RuleIdentity))
            {
                sink.Omit(
                    UnsupportedValidation(concept, resolved.RuleIdentity, "has no rule identity", resolved.Variants.SelectMany(_ => _.Evidence)),
                    [.. resolved.Variants.Select(_ => GenerationFactSemanticKey.ConceptValidationRule(_.Definition))]);
                continue;
            }

            if (resolved.IsConflicted)
            {
                continue;
            }

            var variant = resolved.Variants.Single();
            var definition = variant.Definition;
            var problem = RuleProblem(concept, definition, variant.Evidence);
            if (problem is not null)
            {
                sink.Omit(problem, GenerationFactSemanticKey.ConceptValidationRule(definition));
                continue;
            }

            rules.Add(new ValidationRuleSyntax(
                ValidationRuleSyntax.ConceptValue,
                ValidationRuleKind.Rule,
                new PathExpressionSyntax(definition.Predicate!, Generated),
                definition.Message,
                Generated,
                FileFrom(definition.ImplementationFile)));
            sink.Coverage.Lowered(GenerationFactSemanticKey.ConceptValidationRule(definition));
        }

        return rules.Count == 0
            ? []
            : [new DeclarativeValidateSyntax(rules, Generated)];
    }

    static GenerationDiagnostic? RuleProblem(ArtifactDefinition concept, ConceptValidationRuleDefinition definition, IEnumerable<Evidence> evidence)
    {
        if (definition.Kind == ConceptValidationRuleKind.Unknown || !Enum.IsDefined(definition.Kind))
        {
            return Attach(
                Warning(
                    GenerationDiagnosticCodes.UnsupportedConceptValidationRuleKind,
                    OutcomeFor(definition.Kind, ConceptValidationRuleKind.Unknown),
                    $"Concept '{concept.Name}' validation rule '{definition.RuleIdentity}' uses unknown or undefined ConceptValidationRuleKind value '{(int)definition.Kind}' and was omitted"),
                concept,
                evidence);
        }

        return HasNamedPredicate(definition) && IsValidImplementationFile(definition.ImplementationFile)
            ? null
            : UnsupportedValidation(concept, definition.RuleIdentity, "has invalid or missing required data", evidence);
    }

    static GenerationDiagnostic UnsupportedValidation(ArtifactDefinition concept, string? ruleIdentity, string reason, IEnumerable<Evidence> evidence) =>
        Attach(
            Warning(
                GenerationDiagnosticCodes.UnsupportedConceptValidationRule,
                GenerationDiagnosticOutcome.Unsupported,
                $"Concept '{concept.Name}' validation rule '{ruleIdentity ?? string.Empty}' {reason} and was omitted"),
            concept,
            evidence);

    static GenerationDiagnostic Attach(GenerationDiagnostic diagnostic, ArtifactDefinition concept, IEnumerable<Evidence> evidence) =>
        diagnostic with { Source = FirstSource(evidence), Subject = concept.Key.Subject };

    static bool HasNamedPredicate(ConceptValidationRuleDefinition definition) =>
        definition.Kind == ConceptValidationRuleKind.NamedPredicate &&
        definition.Predicate is { } predicate &&
        IsRuleIdentifier(predicate);

    static bool HasValidEnumerationValues(IReadOnlyList<string> values) =>
        values.All(IsEnumValue) && values.Distinct(StringComparer.Ordinal).Count() == values.Count;

    static string EnumValue(string value) => value.Length == 0
        ? value
        : $"{char.ToLowerInvariant(value[0])}{value[1..]}";

    static bool IsIdentifier(string value) =>
        value.Length > 0 &&
        (value[0] == '_' || char.IsLower(value[0])) &&
        value.Skip(1).All(_ => _ == '_' || char.IsLetterOrDigit(_));

    static bool IsEnumValue(string value) =>
        IsIdentifier(value) &&
        !string.Equals(value, "file", StringComparison.Ordinal) &&
        !string.Equals(value, "validate", StringComparison.Ordinal);

    static bool IsRuleIdentifier(string value) =>
        value.Length > 0 &&
        (value[0] == '_' || char.IsAsciiLetter(value[0])) &&
        value.Skip(1).All(_ => _ == '_' || char.IsAsciiLetterOrDigit(_));

    static bool IsValidImplementationFile(string? value) =>
        value is null ||
        (!string.IsNullOrWhiteSpace(value) && value.IndexOfAny(['\r', '\n']) < 0);

    static string? TypeOf(ConceptRepresentationDefinition representation) => representation.Kind switch
    {
        ConceptRepresentationKind.Primitive when representation.Primitive is not null && representation.EnumerationValues.Count == 0 =>
            _primitiveTypes.GetValueOrDefault(representation.Primitive.Value),
        ConceptRepresentationKind.Enumeration when representation.Primitive is null && representation.EnumerationValues.Count > 0 => "Enum",
        _ => null
    };

    sealed record ConceptCheck(string? Type, IReadOnlyList<string> Values, GenerationDiagnostic? Problem);
}
