// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ResolverDiagnosticCoverage
{
    public static void Propagate(
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        PropagateConflicts(graph, coverage);
        PropagateRejectedSpecifications(graph, coverage);
    }

    static void PropagateConflicts(
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        foreach (var artifact in graph.Artifacts.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingArtifact,
                $"Artifact '{artifact.Key.Subject.Value}' has {artifact.Variants.Count} incompatible {artifact.Key.Kind} definitions",
                artifact.Key.Subject);
            AddConflict(artifact.Variants.Select(_ => GenerationFactSemanticKey.Artifact(_.Definition)), diagnostic, coverage);
        }

        foreach (var representation in graph.ConceptRepresentations.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingConceptRepresentation,
                $"Concept '{representation.Concept.Value}' has {representation.Variants.Count} incompatible representations",
                representation.Concept);
            AddConflict(
                representation.Variants.Select(_ => GenerationFactSemanticKey.ConceptRepresentation(_.Definition)),
                diagnostic,
                coverage);
        }

        foreach (var attribute in graph.ConceptAttributes.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingConceptAttribute,
                $"Concept '{attribute.Concept.Value}' has {attribute.Variants.Count} incompatible '{attribute.Name}' attribute definitions",
                attribute.Concept);
            AddConflict(
                attribute.Variants.Select(_ => GenerationFactSemanticKey.ConceptAttribute(_.Definition)),
                diagnostic,
                coverage);
        }

        foreach (var rule in graph.ConceptValidationRules.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingConceptValidationRule,
                $"Concept '{rule.Concept.Value}' has {rule.Variants.Count} incompatible validation definitions for rule identity '{rule.RuleIdentity}'",
                rule.Concept);
            AddConflict(
                rule.Variants.Select(_ => GenerationFactSemanticKey.ConceptValidationRule(_.Definition)),
                diagnostic,
                coverage);
        }

        foreach (var placement in graph.Placements.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingPlacement,
                $"Artifact '{placement.Artifact.Subject.Value}' has {placement.EffectiveVariants.Count} equally strong incompatible {placement.Artifact.Kind} placements",
                placement.Artifact.Subject);
            AddConflict(
                placement.EffectiveVariants.Select(_ => GenerationFactSemanticKey.Placement(placement.Artifact, _.Placement)),
                diagnostic,
                coverage);
        }

        foreach (var relationship in graph.Relationships.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingRelationship,
                $"Relationship '{relationship.Key.Kind}' from '{relationship.Key.Source.Value}' to '{relationship.Key.Target.Value}' has {relationship.Definitions.Count} incompatible definitions",
                relationship.Key.Source);
            AddConflict(
                relationship.Definitions.Select(GenerationFactSemanticKey.Relationship),
                diagnostic,
                coverage);
        }

        foreach (var scenario in graph.SpecificationScenarios.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingSpecificationScenario,
                $"Specification scenario '{scenario.Key.Scenario.Value}' has {scenario.Variants.Count} incompatible definitions",
                scenario.Key.Scenario);
            AddConflict(
                scenario.Variants.Select(_ => GenerationFactSemanticKey.SpecificationScenario(_.Definition)),
                diagnostic,
                coverage);
        }

        foreach (var step in graph.SpecificationSteps.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingSpecificationStep,
                $"Specification step '{step.Key.Index}' in '{step.Key.Scenario.Scenario.Value}' has {step.Variants.Count} incompatible definitions",
                step.Key.Scenario.Scenario);
            AddConflict(
                step.Variants.Select(_ => GenerationFactSemanticKey.SpecificationStep(_.Definition)),
                diagnostic,
                coverage);
        }

        foreach (var value in graph.SpecificationValues.Where(_ => _.IsConflicted))
        {
            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.ConflictingSpecificationValue,
                $"Specification value '{string.Join('.', value.Key.Path)}' in step '{value.Key.Step.Index}' has {value.Variants.Count} incompatible definitions",
                value.Key.Step.Scenario.Scenario);
            AddConflict(
                value.Variants.Select(_ => GenerationFactSemanticKey.SpecificationValue(_.Definition)),
                diagnostic,
                coverage);
        }
    }

    static void PropagateRejectedSpecifications(
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var admitted = graph.Specifications
            .Select(_ => Structural.SpecificationScenario(_.Definition))
            .ToHashSet(StringComparer.Ordinal);
        var steps = graph.SpecificationSteps.ToDictionary(
            _ => Structural.SpecificationStepKey(_.Key),
            StringComparer.Ordinal);
        var values = graph.SpecificationValues.ToDictionary(
            _ => Structural.SpecificationValueKey(_.Key),
            StringComparer.Ordinal);

        foreach (var scenario in graph.SpecificationScenarios.Where(_ => !_.IsConflicted))
        {
            var definition = scenario.Variants.Single().Definition;
            if (admitted.Contains(Structural.SpecificationScenario(definition)))
            {
                continue;
            }

            var diagnostic = Find(
                graph,
                GenerationDiagnosticCodes.IncompleteSpecificationScenario,
                $"Specification scenario '{scenario.Key.Scenario.Value}' could not be represented completely; no partial scenario was admitted",
                scenario.Key.Scenario);
            if (diagnostic is null)
            {
                continue;
            }

            coverage.Omitted(GenerationFactSemanticKey.SpecificationScenario(definition), diagnostic);
            foreach (var stepKey in definition.Steps)
            {
                if (!steps.TryGetValue(Structural.SpecificationStepKey(stepKey), out var step))
                {
                    continue;
                }

                foreach (var stepVariant in step.Variants)
                {
                    coverage.Omitted(GenerationFactSemanticKey.SpecificationStep(stepVariant.Definition), diagnostic);
                    foreach (var valueKey in stepVariant.Definition.Values)
                    {
                        PropagateValue(valueKey, diagnostic, values, coverage, []);
                    }
                }
            }
        }
    }

    static void PropagateValue(
        SpecificationValueKey key,
        GenerationDiagnostic diagnostic,
        IReadOnlyDictionary<string, ResolvedSpecificationValue> values,
        ScreenplayLoweringCoverageBuilder coverage,
        HashSet<string> visited)
    {
        var keyIdentity = Structural.SpecificationValueKey(key);
        if (!visited.Add(keyIdentity) || !values.TryGetValue(keyIdentity, out var value))
        {
            return;
        }

        foreach (var variant in value.Variants)
        {
            coverage.Omitted(GenerationFactSemanticKey.SpecificationValue(variant.Definition), diagnostic);
            foreach (var child in variant.Definition.Children)
            {
                PropagateValue(child, diagnostic, values, coverage, visited);
            }
        }
    }

    static void AddConflict(
        IEnumerable<string> keys,
        GenerationDiagnostic? diagnostic,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        if (diagnostic is null)
        {
            return;
        }

        foreach (var key in keys)
        {
            coverage.Conflicted(key, diagnostic);
        }
    }

    static GenerationDiagnostic? Find(
        ResolvedApplicationGraph graph,
        string code,
        string message,
        SubjectId subject) =>
        graph.Diagnostics
            .Where(_ =>
                _.Code == code &&
                _.Message == message &&
                _.Subject == subject)
            .OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .FirstOrDefault();
}
