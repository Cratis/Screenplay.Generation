// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

internal static class GenerationFactDispositionCalculator
{
    public static ImmutableArray<GenerationFactRecord> Calculate(
        IEnumerable<GenerationFact> facts,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var admitted = facts.ToArray();
        var conflictingIdentities = admitted
            .Where(HasSupportedDiscriminators)
            .GroupBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Where(group => group
                .Select(SemanticIdentity)
                .Distinct(StringComparer.Ordinal)
                .Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);

        return
        [
            .. admitted.Select(fact => Calculate(
                fact,
                conflictingIdentities.Contains(fact.Id.Value),
                graph,
                coverage,
                diagnostics))
        ];
    }

    static GenerationFactRecord Calculate(
        GenerationFact fact,
        bool hasConflictingIdentity,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        if (!HasSupportedDiscriminators(fact))
        {
            return Omitted(fact, coverage, diagnostics);
        }

        if (hasConflictingIdentity || IsConflicted(fact, graph, coverage))
        {
            return Conflicted(fact, coverage, diagnostics);
        }

        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && coverage.Lowered.Contains(key))
        {
            return new GenerationFactRecord
            {
                Fact = fact,
                Disposition = GenerationFactDisposition.Lowered
            };
        }

        if (fact is ArtifactPlacementFact placement && IsWeakerPlacement(placement, graph))
        {
            return new GenerationFactRecord
            {
                Fact = fact,
                Disposition = GenerationFactDisposition.ProvenanceOnly
            };
        }

        if (key is not null)
        {
            return Omitted(fact, coverage, diagnostics);
        }

        var diagnostic = new GenerationDiagnostic
        {
            Code = GenerationDiagnosticCodes.UnclassifiedGenerationFact,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = $"Admitted fact '{fact.Id.Value}' uses unclassifiable fact type '{fact.GetType().FullName}'",
            Source = fact.Evidence.Source,
            Subject = fact.Subject
        };
        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Unknown,
            Diagnostics = [diagnostic]
        };
    }

    static GenerationFactRecord Conflicted(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var associated = AssociatedDiagnostics(fact, coverage, diagnostics)
            .Where(_ => _.Outcome == GenerationDiagnosticOutcome.Conflict)
            .ToArray();
        if (associated.Length == 0)
        {
            associated =
            [
                new GenerationDiagnostic
                {
                    Code = GenerationDiagnosticCodes.ConflictingGenerationFact,
                    Severity = GenerationDiagnosticSeverity.Error,
                    Outcome = GenerationDiagnosticOutcome.Conflict,
                    Message = $"Admitted fact '{fact.Id.Value}' participated in an unresolved semantic conflict",
                    Source = fact.Evidence.Source,
                    Subject = DiagnosticSubject(fact)
                }
            ];
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.Conflicted,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static GenerationFactRecord Omitted(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var associated = AssociatedDiagnostics(fact, coverage, diagnostics).ToList();
        if (fact is RelationshipFact relationship && HasSupportedDiscriminators(fact))
        {
            associated.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnsupportedRelationship,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Relationship '{relationship.Definition.Key.Kind}' from '{relationship.Definition.Key.Source.Value}' to '{relationship.Definition.Key.Target.Value}' did not contribute to emitted Screenplay syntax and was omitted",
                Source = fact.Evidence.Source,
                Subject = relationship.Definition.Key.Source
            });
        }

        if (associated.Count == 0)
        {
            associated.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.OmittedGenerationFact,
                Severity = GenerationDiagnosticSeverity.Warning,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Admitted {FactFamily(fact)} fact '{fact.Id.Value}' did not contribute to emitted Screenplay syntax and was omitted",
                Source = fact.Evidence.Source,
                Subject = DiagnosticSubject(fact)
            });
        }

        return new GenerationFactRecord
        {
            Fact = fact,
            Disposition = GenerationFactDisposition.OmittedWithDiagnostic,
            Diagnostics = CanonicalDiagnostics(associated)
        };
    }

    static IEnumerable<GenerationDiagnostic> AssociatedDiagnostics(
        GenerationFact fact,
        ScreenplayLoweringCoverage coverage,
        IReadOnlyList<GenerationDiagnostic> diagnostics)
    {
        var key = GenerationFactSemanticKey.For(fact);
        if (key is not null && coverage.Diagnostics.TryGetValue(key, out var loweringDiagnostics))
        {
            foreach (var diagnostic in loweringDiagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in diagnostics.Where(_ =>
                     _.Outcome is not null && HasExactFactIdentity(_.Message, fact.Id.Value)))
        {
            yield return diagnostic;
        }
    }

    static bool IsConflicted(
        GenerationFact fact,
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverage coverage)
    {
        var semanticKey = GenerationFactSemanticKey.For(fact);
        if (semanticKey is not null && coverage.Conflicted.Contains(semanticKey))
        {
            return true;
        }

        return fact switch
        {
            ArtifactFact artifact => graph.Artifacts.Any(resolved =>
                resolved.IsConflicted &&
                Structural.ArtifactKey(resolved.Key) == Structural.ArtifactKey(artifact.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.Artifact(variant.Definition) == Structural.Artifact(artifact.Definition))),
            ArtifactPlacementFact placement => ConflictingPlacement(placement, graph),
            RelationshipFact relationship => graph.Relationships.Any(resolved =>
                resolved.IsConflicted &&
                Structural.RelationshipKey(resolved.Key) == Structural.RelationshipKey(relationship.Definition.Key) &&
                resolved.Definitions.Any(definition => Structural.Relationship(definition) == Structural.Relationship(relationship.Definition))),
            ConceptRepresentationFact representation => graph.ConceptRepresentations.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Concept == representation.Definition.Concept &&
                resolved.Variants.Any(variant => Structural.ConceptRepresentation(variant.Definition) == Structural.ConceptRepresentation(representation.Definition))),
            ConceptAttributeFact attribute => graph.ConceptAttributes.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Variants.Any(variant => Structural.ConceptAttribute(variant.Definition) == Structural.ConceptAttribute(attribute.Definition))),
            ConceptValidationRuleFact validation => graph.ConceptValidationRules.Any(resolved =>
                resolved.IsConflicted &&
                resolved.Variants.Any(variant => Structural.ConceptValidationRule(variant.Definition) == Structural.ConceptValidationRule(validation.Definition))),
            SpecificationScenarioFact scenario => graph.SpecificationScenarios.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationScenarioKey(resolved.Key) == Structural.SpecificationScenarioKey(scenario.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationScenario(variant.Definition) == Structural.SpecificationScenario(scenario.Definition))),
            SpecificationStepFact step => graph.SpecificationSteps.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationStepKey(resolved.Key) == Structural.SpecificationStepKey(step.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationStep(variant.Definition) == Structural.SpecificationStep(step.Definition))),
            SpecificationValueFact value => graph.SpecificationValues.Any(resolved =>
                resolved.IsConflicted &&
                Structural.SpecificationValueKey(resolved.Key) == Structural.SpecificationValueKey(value.Definition.Key) &&
                resolved.Variants.Any(variant => Structural.SpecificationValue(variant.Definition) == Structural.SpecificationValue(value.Definition))),
            _ => false
        };
    }

    static bool ConflictingPlacement(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
    {
        var resolved = graph.Placements.FirstOrDefault(_ =>
            Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(fact.Artifact));
        if (resolved?.IsConflicted != true)
        {
            return false;
        }

        var placement = Structural.Placement(fact.Placement);
        return resolved.EffectiveVariants.Any(_ => Structural.Placement(_.Placement) == placement);
    }

    static bool IsWeakerPlacement(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
    {
        var resolved = graph.Placements.FirstOrDefault(_ =>
            Structural.ArtifactKey(_.Artifact) == Structural.ArtifactKey(fact.Artifact));
        if (resolved?.Variants.Any(_ => Structural.Placement(_.Placement) == Structural.Placement(fact.Placement)) != true)
        {
            return false;
        }

        var placement = Structural.Placement(fact.Placement);
        return resolved.EffectiveVariants.All(_ => Structural.Placement(_.Placement) != placement);
    }

    static bool HasExactFactIdentity(string message, string factId) =>
        message.Contains($"Fact '{factId}'", StringComparison.Ordinal) ||
        message.Contains($"fact '{factId}'", StringComparison.Ordinal) ||
        message.Contains($"Fact identity '{factId}'", StringComparison.Ordinal);

    static bool HasSupportedDiscriminators(GenerationFact fact)
    {
        if (!Supported(fact.Evidence.Strength, EvidenceStrength.Unknown))
        {
            return false;
        }

        return fact switch
        {
            ArtifactFact artifact => Supported(artifact.Definition.Key.Kind, ArtifactKind.Unknown),
            ArtifactPlacementFact placement =>
                Supported(placement.Artifact.Kind, ArtifactKind.Unknown) &&
                Supported(placement.Placement.SliceKind, GenerationSliceKind.Unknown),
            RelationshipFact relationship => Supported(relationship.Definition.Key.Kind, RelationshipKind.Unknown),
            ConceptRepresentationFact representation =>
                Supported(representation.Definition.Kind, ConceptRepresentationKind.Unknown) &&
                (representation.Definition.Primitive is not { } primitive || Supported(primitive, GenerationPrimitiveKind.Unknown)),
            ConceptAttributeFact attribute => Supported(attribute.Definition.Kind, ConceptAttributeKind.Unknown),
            ConceptValidationRuleFact validation => Supported(validation.Definition.Kind, ConceptValidationRuleKind.Unknown),
            SpecificationScenarioFact scenario => Supported(scenario.Definition.TargetArtifact.Kind, ArtifactKind.Unknown),
            SpecificationStepFact step =>
                Supported(step.Definition.Phase, SpecificationStepPhase.Unknown) &&
                Supported(step.Definition.Kind, SpecificationStepKind.Unknown) &&
                (step.Definition.Artifact is not { } artifact || Supported(artifact.Kind, ArtifactKind.Unknown)),
            SpecificationValueFact value => Supported(value.Definition.Kind, SpecificationValueKind.Unknown),
            _ => true
        };
    }

    static bool Supported<TEnum>(TEnum value, TEnum unknown)
        where TEnum : struct, Enum =>
        !EqualityComparer<TEnum>.Default.Equals(value, unknown) && Enum.IsDefined(value);

    static SubjectId DiagnosticSubject(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Key.Subject,
        ArtifactPlacementFact placement => placement.Artifact.Subject,
        RelationshipFact relationship => relationship.Definition.Key.Source,
        ConceptRepresentationFact representation => representation.Definition.Concept,
        ConceptAttributeFact attribute => attribute.Definition.Concept,
        ConceptValidationRuleFact validation => validation.Definition.Concept,
        SpecificationScenarioFact scenario => scenario.Definition.Key.Scenario,
        SpecificationStepFact step => step.Definition.Key.Scenario.Scenario,
        SpecificationValueFact value => value.Definition.Key.Step.Scenario.Scenario,
        _ => fact.Subject
    };

    static string SemanticIdentity(GenerationFact fact) =>
        GenerationFactSemanticKey.For(fact) ?? fact.GetType().FullName ?? fact.GetType().Name;

    static string FactFamily(GenerationFact fact) => fact switch
    {
        ArtifactFact => "artifact",
        ArtifactPlacementFact => "artifact placement",
        RelationshipFact => "relationship",
        ConceptRepresentationFact => "concept representation",
        ConceptAttributeFact => "concept attribute",
        ConceptValidationRuleFact => "concept validation rule",
        SpecificationScenarioFact => "specification scenario",
        SpecificationStepFact => "specification step",
        SpecificationValueFact => "specification value",
        _ => "generation"
    };

    static ImmutableArray<GenerationDiagnostic> CanonicalDiagnostics(IEnumerable<GenerationDiagnostic> diagnostics) =>
    [
        .. diagnostics
            .GroupBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.First())
    ];
}
