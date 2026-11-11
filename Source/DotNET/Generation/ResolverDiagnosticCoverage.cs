// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ResolverDiagnosticCoverage
{
    public static void Propagate(
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var propagation = new ConflictPropagation(graph, coverage);
        PropagateIdentityConflicts(propagation);
        PropagateSpecificationConflicts(propagation);
        PropagateRejectedSpecifications(graph, coverage);
    }

    static void PropagateIdentityConflicts(ConflictPropagation propagation)
    {
        foreach (var artifact in propagation.Graph.Artifacts.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingArtifact,
                $"Artifact '{artifact.Key.Subject.Value}' has {artifact.Variants.Count} incompatible {artifact.Key.Kind} definitions",
                artifact.Key.Subject,
                artifact.Variants.Select(_ => GenerationFactSemanticKey.Artifact(_.Definition)));
        }

        foreach (var representation in propagation.Graph.ConceptRepresentations.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingConceptRepresentation,
                $"Concept '{representation.Concept.Value}' has {representation.Variants.Count} incompatible representations",
                representation.Concept,
                representation.Variants.Select(_ => GenerationFactSemanticKey.ConceptRepresentation(_.Definition)));
        }

        foreach (var attribute in propagation.Graph.ConceptAttributes.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingConceptAttribute,
                $"Concept '{attribute.Concept.Value}' has {attribute.Variants.Count} incompatible '{attribute.Name}' attribute definitions",
                attribute.Concept,
                attribute.Variants.Select(_ => GenerationFactSemanticKey.ConceptAttribute(_.Definition)));
        }

        foreach (var rule in propagation.Graph.ConceptValidationRules.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingConceptValidationRule,
                $"Concept '{rule.Concept.Value}' has {rule.Variants.Count} incompatible validation definitions for rule identity '{rule.RuleIdentity}'",
                rule.Concept,
                rule.Variants.Select(_ => GenerationFactSemanticKey.ConceptValidationRule(_.Definition)));
        }

        foreach (var placement in propagation.Graph.Placements.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingPlacement,
                $"Artifact '{placement.Artifact.Subject.Value}' has {placement.EffectiveVariants.Count} equally strong incompatible {placement.Artifact.Kind} placements",
                placement.Artifact.Subject,
                placement.EffectiveVariants.Select(_ => GenerationFactSemanticKey.Placement(placement.Artifact, _.Placement)));
        }

        foreach (var relationship in propagation.Graph.Relationships.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingRelationship,
                $"Relationship '{relationship.Key.Kind}' from '{relationship.Key.Source.Value}' to '{relationship.Key.Target.Value}' has {relationship.Definitions.Count} incompatible definitions",
                relationship.Key.Source,
                relationship.Definitions.Select(GenerationFactSemanticKey.Relationship));
        }
    }

    static void PropagateSpecificationConflicts(ConflictPropagation propagation)
    {
        foreach (var scenario in propagation.Graph.SpecificationScenarios.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingSpecificationScenario,
                $"Specification scenario '{scenario.Key.Scenario.Value}' has {scenario.Variants.Count} incompatible definitions",
                scenario.Key.Scenario,
                scenario.Variants.Select(_ => GenerationFactSemanticKey.SpecificationScenario(_.Definition)));
        }

        foreach (var step in propagation.Graph.SpecificationSteps.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingSpecificationStep,
                $"Specification step '{step.Key.Index}' in '{step.Key.Scenario.Scenario.Value}' has {step.Variants.Count} incompatible definitions",
                step.Key.Scenario.Scenario,
                step.Variants.Select(_ => GenerationFactSemanticKey.SpecificationStep(_.Definition)));
        }

        foreach (var value in propagation.Graph.SpecificationValues.Where(_ => _.IsConflicted))
        {
            propagation.Add(
                GenerationDiagnosticCodes.ConflictingSpecificationValue,
                $"Specification value '{string.Join('.', value.Key.Path)}' in step '{value.Key.Step.Index}' has {value.Variants.Count} incompatible definitions",
                value.Key.Step.Scenario.Scenario,
                value.Variants.Select(_ => GenerationFactSemanticKey.SpecificationValue(_.Definition)));
        }
    }

    static void PropagateRejectedSpecifications(
        ResolvedApplicationGraph graph,
        ScreenplayLoweringCoverageBuilder coverage)
    {
        var admitted = graph.Specifications
            .Select(_ => Structural.SpecificationScenario(_.Definition))
            .ToHashSet(StringComparer.Ordinal);
        var omission = new SpecificationOmission(
            coverage,
            graph.SpecificationSteps.ToDictionary(
                _ => Structural.SpecificationStepKey(_.Key),
                StringComparer.Ordinal),
            graph.SpecificationValues.ToDictionary(
                _ => Structural.SpecificationValueKey(_.Key),
                StringComparer.Ordinal));

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
            if (diagnostic is not null)
            {
                omission.OmitScenario(definition, diagnostic);
            }
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

    sealed record ConflictPropagation(
        ResolvedApplicationGraph Graph,
        ScreenplayLoweringCoverageBuilder Coverage)
    {
        public void Add(string code, string message, SubjectId subject, IEnumerable<string> keys)
        {
            var diagnostic = Find(Graph, code, message, subject);
            if (diagnostic is null)
            {
                return;
            }

            foreach (var key in keys)
            {
                Coverage.Conflicted(key, diagnostic);
            }
        }
    }

    sealed record SpecificationOmission(
        ScreenplayLoweringCoverageBuilder Coverage,
        IReadOnlyDictionary<string, ResolvedSpecificationStep> Steps,
        IReadOnlyDictionary<string, ResolvedSpecificationValue> Values)
    {
        public void OmitScenario(SpecificationScenarioDefinition definition, GenerationDiagnostic diagnostic)
        {
            Coverage.Omitted(GenerationFactSemanticKey.SpecificationScenario(definition), diagnostic);
            foreach (var stepKey in definition.Steps)
            {
                if (Steps.TryGetValue(Structural.SpecificationStepKey(stepKey), out var step))
                {
                    OmitStep(step, diagnostic);
                }
            }
        }

        void OmitStep(ResolvedSpecificationStep step, GenerationDiagnostic diagnostic)
        {
            foreach (var stepVariant in step.Variants)
            {
                Coverage.Omitted(GenerationFactSemanticKey.SpecificationStep(stepVariant.Definition), diagnostic);
                foreach (var valueKey in stepVariant.Definition.Values)
                {
                    OmitValue(valueKey, diagnostic, []);
                }
            }
        }

        void OmitValue(SpecificationValueKey key, GenerationDiagnostic diagnostic, HashSet<string> visited)
        {
            var keyIdentity = Structural.SpecificationValueKey(key);
            if (!visited.Add(keyIdentity) || !Values.TryGetValue(keyIdentity, out var value))
            {
                return;
            }

            foreach (var variant in value.Variants)
            {
                Coverage.Omitted(GenerationFactSemanticKey.SpecificationValue(variant.Definition), diagnostic);
                foreach (var child in variant.Definition.Children)
                {
                    OmitValue(child, diagnostic, visited);
                }
            }
        }
    }
}
