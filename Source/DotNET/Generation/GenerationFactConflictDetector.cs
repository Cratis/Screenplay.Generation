// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Decides whether an admitted fact took part in a conflict the resolver could not settle.
/// </summary>
internal static class GenerationFactConflictDetector
{
    public static bool IsConflicted(
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
            ArtifactFact or
            ArtifactPlacementFact or
            ArtifactDeclarationFact or
            ArtifactMemberDeclarationFact or
            ArtifactMemberTypeUseFact or
            TypeUseBindingFact or
            ArtifactMemberRoleFact => ArtifactFamilyConflicted(fact, graph),
            RelationshipFact relationship => RelationshipConflicted(relationship, graph),
            ConceptRepresentationFact or
            ConceptAttributeFact or
            ConceptValidationRuleFact => ConceptFamilyConflicted(fact, graph),
            SpecificationScenarioFact or
            SpecificationStepFact or
            SpecificationValueFact => SpecificationFamilyConflicted(fact, graph),
            _ => false
        };
    }

    static bool ArtifactFamilyConflicted(GenerationFact fact, ResolvedApplicationGraph graph) => fact switch
    {
        ArtifactFact artifact => ArtifactConflicted(artifact, graph),
        ArtifactPlacementFact placement => PlacementConflicted(placement, graph),
        _ => ArtifactMemberConflicted(fact, graph)
    };

    static bool ArtifactConflicted(ArtifactFact artifact, ResolvedApplicationGraph graph) =>
        graph.Artifacts.Any(resolved =>
            resolved.IsConflicted &&
            Structural.ArtifactKey(resolved.Key) == Structural.ArtifactKey(artifact.Definition.Key) &&
            resolved.Variants.Any(variant => variant.SupportingFacts.Any(id => id == artifact.Id)));

    static bool ArtifactMemberConflicted(GenerationFact fact, ResolvedApplicationGraph graph) =>
        graph.Artifacts.Any(resolved =>
            resolved.IsConflicted &&
            resolved.Variants.Any(variant => variant.SupportingFacts.Any(id => id == fact.Id)));

    static bool PlacementConflicted(ArtifactPlacementFact fact, ResolvedApplicationGraph graph)
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

    static bool RelationshipConflicted(RelationshipFact relationship, ResolvedApplicationGraph graph) =>
        graph.Relationships.Any(resolved =>
            resolved.IsConflicted &&
            Structural.RelationshipKey(resolved.Key) == Structural.RelationshipKey(relationship.Definition.Key) &&
            resolved.Definitions.Any(definition => Structural.Relationship(definition) == Structural.Relationship(relationship.Definition)));

    static bool ConceptFamilyConflicted(GenerationFact fact, ResolvedApplicationGraph graph) => fact switch
    {
        ConceptRepresentationFact representation => RepresentationConflicted(representation, graph),
        ConceptAttributeFact attribute => AttributeConflicted(attribute, graph),
        ConceptValidationRuleFact validation => ValidationRuleConflicted(validation, graph),
        _ => false
    };

    static bool RepresentationConflicted(ConceptRepresentationFact representation, ResolvedApplicationGraph graph) =>
        graph.ConceptRepresentations.Any(resolved =>
            resolved.IsConflicted &&
            resolved.Concept == representation.Definition.Concept &&
            resolved.Variants.Any(variant => Structural.ConceptRepresentation(variant.Definition) == Structural.ConceptRepresentation(representation.Definition)));

    static bool AttributeConflicted(ConceptAttributeFact attribute, ResolvedApplicationGraph graph) =>
        graph.ConceptAttributes.Any(resolved =>
            resolved.IsConflicted &&
            resolved.Variants.Any(variant => Structural.ConceptAttribute(variant.Definition) == Structural.ConceptAttribute(attribute.Definition)));

    static bool ValidationRuleConflicted(ConceptValidationRuleFact validation, ResolvedApplicationGraph graph) =>
        graph.ConceptValidationRules.Any(resolved =>
            resolved.IsConflicted &&
            resolved.Variants.Any(variant => Structural.ConceptValidationRule(variant.Definition) == Structural.ConceptValidationRule(validation.Definition)));

    static bool SpecificationFamilyConflicted(GenerationFact fact, ResolvedApplicationGraph graph) => fact switch
    {
        SpecificationScenarioFact scenario => ScenarioConflicted(scenario, graph),
        SpecificationStepFact step => StepConflicted(step, graph),
        SpecificationValueFact value => ValueConflicted(value, graph),
        _ => false
    };

    static bool ScenarioConflicted(SpecificationScenarioFact scenario, ResolvedApplicationGraph graph) =>
        graph.SpecificationScenarios.Any(resolved =>
            resolved.IsConflicted &&
            Structural.SpecificationScenarioKey(resolved.Key) == Structural.SpecificationScenarioKey(scenario.Definition.Key) &&
            resolved.Variants.Any(variant => Structural.SpecificationScenario(variant.Definition) == Structural.SpecificationScenario(scenario.Definition)));

    static bool StepConflicted(SpecificationStepFact step, ResolvedApplicationGraph graph) =>
        graph.SpecificationSteps.Any(resolved =>
            resolved.IsConflicted &&
            Structural.SpecificationStepKey(resolved.Key) == Structural.SpecificationStepKey(step.Definition.Key) &&
            resolved.Variants.Any(variant => Structural.SpecificationStep(variant.Definition) == Structural.SpecificationStep(step.Definition)));

    static bool ValueConflicted(SpecificationValueFact value, ResolvedApplicationGraph graph) =>
        graph.SpecificationValues.Any(resolved =>
            resolved.IsConflicted &&
            Structural.SpecificationValueKey(resolved.Key) == Structural.SpecificationValueKey(value.Definition.Key) &&
            resolved.Variants.Any(variant => Structural.SpecificationValue(variant.Definition) == Structural.SpecificationValue(value.Definition)));
}
