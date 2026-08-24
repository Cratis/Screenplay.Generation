// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Generation;

static class Canonical
{
    const char Separator = '\u001f';

    public static string Artifact(ArtifactDefinition definition)
    {
        var properties = string.Join(
            Separator,
            definition.Properties.Select(_ => Join(
                _.Name,
                _.Type.Name,
                _.Type.Subject?.Value,
                _.Type.IsCollection.ToString(),
                _.Type.IsOptional.ToString(),
                _.IsIdentifier.ToString())));

        return Join(
            ArtifactKey(definition.Key),
            definition.Name,
            definition.Description,
            definition.File,
            properties);
    }

    public static string ArtifactKey(ArtifactKey key) => Join(key.Subject.Value, key.Kind.ToString());

    public static string ConceptRepresentation(ConceptRepresentationDefinition definition) =>
        Join(
            definition.Concept.Value,
            definition.Kind.ToString(),
            definition.Primitive?.ToString(),
            string.Concat(definition.EnumerationValues.Select(_ => $"{_.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)}:{_}")));

    public static string ConceptAttribute(ConceptAttributeDefinition definition) =>
        Join(definition.Concept.Value, definition.Kind.ToString(), Encode(definition.Name), Encode(definition.Reason));

    public static string ConceptAttributeKey(ConceptAttributeDefinition definition) =>
        Join(definition.Concept.Value, definition.Kind.ToString(), Encode(definition.Name));

    public static string ConceptValidationRule(ConceptValidationRuleDefinition definition) =>
        Join(
            definition.Concept.Value,
            Encode(definition.RuleIdentity),
            definition.Kind.ToString(),
            Encode(definition.Predicate),
            Encode(definition.Message),
            Encode(definition.ImplementationFile));

    public static string ConceptValidationRuleKey(ConceptValidationRuleDefinition definition) =>
        Join(definition.Concept.Value, Encode(definition.RuleIdentity));

    public static string Placement(ArtifactPlacement placement) =>
        Join(
            placement.Module,
            string.Join(Separator, placement.Features),
            placement.Slice,
            placement.SliceKind.ToString());

    public static string Relationship(RelationshipDefinition definition) =>
        Join(
            RelationshipKey(definition.Key),
            definition.SourceMember,
            definition.TargetMember,
            definition.IsCollection.ToString(),
            definition.IsOptional.ToString());

    public static string RelationshipKey(RelationshipKey key) =>
        Join(key.Kind.ToString(), key.Source.Value, key.Target.Value, key.Discriminator);

    public static string SpecificationScenarioKey(SpecificationScenarioKey key) =>
        Structural(key.Scenario.Value);

    public static string SpecificationStepKey(SpecificationStepKey key) =>
        Structural(SpecificationScenarioKey(key.Scenario), Invariant(key.Index));

    public static string SpecificationValueKey(SpecificationValueKey key) =>
        Structural(SpecificationStepKey(key.Step), Structural([.. key.Path]));

    public static string SpecificationScenario(SpecificationScenarioDefinition definition) =>
        Structural(
            SpecificationScenarioKey(definition.Key),
            definition.Name,
            ArtifactKey(definition.TargetArtifact),
            Structural([.. definition.Steps.Select(SpecificationStepKey)]));

    public static string SpecificationStep(SpecificationStepDefinition definition) =>
        Structural(
            SpecificationStepKey(definition.Key),
            Invariant((int)definition.Phase),
            Invariant((int)definition.Kind),
            definition.Artifact is null ? null : ArtifactKey(definition.Artifact),
            definition.ErrorCode,
            definition.ErrorMessage,
            Structural([.. definition.Values.Select(SpecificationValueKey)]));

    public static string SpecificationValue(SpecificationValueDefinition definition) =>
        Structural(
            SpecificationValueKey(definition.Key),
            Invariant((int)definition.Kind),
            TypeReference(definition.Type),
            definition.Scalar,
            Structural([.. definition.Children.Select(SpecificationValueKey)]));

    public static string Evidence(Evidence evidence)
    {
        var identity = evidence.Source?.FileIdentity;

        return Structural(
            evidence.Adapter.Id,
            evidence.Adapter.Version,
            Invariant((int)evidence.Strength),
            evidence.Source?.Path,
            Invariant(evidence.Source?.StartLine),
            Invariant(evidence.Source?.StartColumn),
            Invariant(evidence.Source?.EndLine),
            Invariant(evidence.Source?.EndColumn),
            evidence.Explanation,
            identity is null ? "0" : "1",
            identity?.Project,
            identity?.Path);
    }

    public static string Diagnostic(GenerationDiagnostic diagnostic)
    {
        var identity = diagnostic.Source?.FileIdentity;
        int? outcome = diagnostic.Outcome is null ? null : (int)diagnostic.Outcome.Value;

        return Structural(
            Invariant((int)diagnostic.Severity, "D2"),
            diagnostic.Code,
            diagnostic.Source?.Path,
            Invariant(diagnostic.Source?.StartLine),
            Invariant(diagnostic.Source?.StartColumn),
            Invariant(diagnostic.Source?.EndLine),
            Invariant(diagnostic.Source?.EndColumn),
            diagnostic.Subject?.Value,
            diagnostic.Message,
            Invariant(outcome),
            identity is null ? "0" : "1",
            identity?.Project,
            identity?.Path);
    }

    static string? TypeReference(TypeReferenceDefinition? type) => type is null
        ? null
        : Structural(
            type.Name,
            type.Subject?.Value,
            type.IsCollection.ToString(),
            type.IsOptional.ToString());

    static string Encode(string? value) => value is null
        ? "-1:"
        : $"{value.Length.ToString(CultureInfo.InvariantCulture)}:{value}";

    static string Invariant(int value, string? format = null) => value.ToString(format, CultureInfo.InvariantCulture);

    static string? Invariant(int? value, string? format = null) => value?.ToString(format, CultureInfo.InvariantCulture);

    static string Structural(params string?[] values) => string.Join(Separator, values.Select(Encode));

    static string Join(params string?[] values) => string.Join(Separator, values.Select(_ => _ ?? string.Empty));
}
