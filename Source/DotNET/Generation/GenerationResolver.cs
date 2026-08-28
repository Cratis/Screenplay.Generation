// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Resolves adapter contributions into one deterministic semantic application graph.
/// </summary>
public sealed class GenerationResolver
{
    /// <summary>
    /// Resolves the supplied adapter contributions.
    /// </summary>
    /// <param name="contributions">The adapter contributions to resolve.</param>
    /// <returns>The resolved application graph.</returns>
    public ResolvedApplicationGraph Resolve(IEnumerable<AdapterContribution> contributions)
    {
        var orderedContributions = contributions
            .OrderBy(_ => _.Adapter.Id, StringComparer.Ordinal)
            .ThenBy(_ => _.Adapter.Version, StringComparer.Ordinal)
            .ToArray();
        var contributedFacts = orderedContributions.SelectMany(_ => _.Facts).ToArray();
        var diagnostics = orderedContributions.SelectMany(_ => _.Diagnostics);

        return ResolveFacts(contributedFacts, diagnostics);
    }

    internal ResolvedApplicationGraph ResolveFacts(
        IEnumerable<GenerationFact> contributedFacts,
        IEnumerable<GenerationDiagnostic> contributedDiagnostics)
    {
        var diagnostics = contributedDiagnostics.ToList();
        var discriminatorValidation = GenerationFactDiscriminatorValidator.Validate(contributedFacts);
        var facts = discriminatorValidation.Facts;
        diagnostics.AddRange(discriminatorValidation.Diagnostics);

        diagnostics.AddRange(ConflictingFactIdentityDiagnostics(facts));

        var effectiveArtifactFacts = GranularArtifactResolver.Resolve(facts, diagnostics);
        var artifacts = ResolveArtifacts(effectiveArtifactFacts, diagnostics);
        var conceptRepresentationFacts = facts.OfType<ConceptRepresentationFact>().ToArray();
        diagnostics.AddRange(InvalidConceptFactDiagnostics(conceptRepresentationFacts));
        var conceptRepresentations = ResolveConceptRepresentations(
            conceptRepresentationFacts.Where(_ => _.Subject == _.Definition.Concept),
            diagnostics);
        var conceptAttributeFacts = facts.OfType<ConceptAttributeFact>().ToArray();
        diagnostics.AddRange(InvalidConceptFactDiagnostics(conceptAttributeFacts));
        var conceptAttributes = ResolveConceptAttributes(
            conceptAttributeFacts.Where(_ => _.Subject == _.Definition.Concept),
            diagnostics);
        var conceptValidationRuleFacts = facts.OfType<ConceptValidationRuleFact>().ToArray();
        diagnostics.AddRange(InvalidConceptFactDiagnostics(conceptValidationRuleFacts));
        var conceptValidationRules = ResolveConceptValidationRules(
            conceptValidationRuleFacts.Where(_ => _.Subject == _.Definition.Concept),
            diagnostics);
        var placements = ResolvePlacements(facts.OfType<ArtifactPlacementFact>(), diagnostics);
        var relationships = ResolveRelationships(facts.OfType<RelationshipFact>(), diagnostics);
        var specificationFacts = SpecificationFactResolver.Resolve(
            facts.OfType<SpecificationScenarioFact>(),
            facts.OfType<SpecificationStepFact>(),
            facts.OfType<SpecificationValueFact>(),
            diagnostics);
        var specifications = SpecificationAdmission.Admit(
            specificationFacts,
            artifacts,
            placements,
            diagnostics);

        return new()
        {
            Artifacts = artifacts,
            ConceptRepresentations = conceptRepresentations,
            ConceptAttributes = conceptAttributes,
            ConceptValidationRules = conceptValidationRules,
            Placements = placements,
            Relationships = relationships,
            SpecificationScenarios = specificationFacts.Scenarios,
            SpecificationSteps = specificationFacts.Steps,
            SpecificationValues = specificationFacts.Values,
            Specifications = specifications,
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]
        };
    }

    static ResolvedArtifact[] ResolveArtifacts(
        IEnumerable<ArtifactFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
            .GroupBy(_ => Structural.ArtifactKey(_.Definition.Key), StringComparer.Ordinal)
            .OrderBy(_ => Canonical.ArtifactKey(_.First().Definition.Key), StringComparer.Ordinal)
            .ThenBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var variants = group
                    .GroupBy(_ => Structural.Artifact(_.Definition), StringComparer.Ordinal)
                    .OrderBy(_ => Canonical.Artifact(_.First().Definition), StringComparer.Ordinal)
                    .ThenBy(_ => _.Key, StringComparer.Ordinal)
                    .Select(_ => new ResolvedArtifactVariant
                    {
                        Definition = _.First().Definition,
                        SupportingFacts =
                        [
                            .. _.Select(fact => fact.Id)
                                .Distinct()
                                .OrderBy(id => id.Value, StringComparer.Ordinal)
                        ],
                        Evidence = OrderedEvidence(_.Select(fact => fact.Evidence))
                    })
                    .ToArray();

                var artifact = new ResolvedArtifact
                {
                    Key = variants[0].Definition.Key,
                    Variants = variants
                };

                if (artifact.IsConflicted)
                {
                    diagnostics.Add(ConflictFor(artifact));
                }

                return artifact;
            })
        ];

    static ResolvedConceptRepresentation[] ResolveConceptRepresentations(
        IEnumerable<ConceptRepresentationFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(_ => _.Definition.Concept.Value, StringComparer.Ordinal)
                .OrderBy(_ => _.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(_ => Structural.ConceptRepresentation(_.Definition), StringComparer.Ordinal)
                        .OrderBy(_ => Canonical.ConceptRepresentation(_.First().Definition), StringComparer.Ordinal)
                        .ThenBy(_ => _.Key, StringComparer.Ordinal)
                        .Select(_ => new ResolvedConceptRepresentationVariant
                        {
                            Definition = _.First().Definition,
                            Evidence = OrderedEvidence(_.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var representation = new ResolvedConceptRepresentation
                    {
                        Concept = group.First().Definition.Concept,
                        Variants = variants
                    };

                    if (representation.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(representation));
                    }

                    return representation;
                })
        ];

    static ResolvedConceptAttribute[] ResolveConceptAttributes(
        IEnumerable<ConceptAttributeFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(_ => Structural.ConceptAttributeKey(_.Definition), StringComparer.Ordinal)
                .OrderBy(_ => Canonical.ConceptAttributeKey(_.First().Definition), StringComparer.Ordinal)
                .ThenBy(_ => _.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(_ => Structural.ConceptAttribute(_.Definition), StringComparer.Ordinal)
                        .OrderBy(_ => Canonical.ConceptAttribute(_.First().Definition), StringComparer.Ordinal)
                        .ThenBy(_ => _.Key, StringComparer.Ordinal)
                        .Select(_ => new ResolvedConceptAttributeVariant
                        {
                            Definition = _.First().Definition,
                            Evidence = OrderedEvidence(_.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var attribute = new ResolvedConceptAttribute
                    {
                        Concept = group.First().Definition.Concept,
                        Name = group.First().Definition.Name,
                        Variants = variants
                    };

                    if (attribute.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(attribute));
                    }

                    return attribute;
                })
        ];

    static ResolvedConceptValidationRule[] ResolveConceptValidationRules(
        IEnumerable<ConceptValidationRuleFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
                .GroupBy(_ => Structural.ConceptValidationRuleKey(_.Definition), StringComparer.Ordinal)
                .OrderBy(_ => Canonical.ConceptValidationRuleKey(_.First().Definition), StringComparer.Ordinal)
                .ThenBy(_ => _.Key, StringComparer.Ordinal)
                .Select(group =>
                {
                    var variants = group
                        .GroupBy(_ => Structural.ConceptValidationRule(_.Definition), StringComparer.Ordinal)
                        .OrderBy(_ => Canonical.ConceptValidationRule(_.First().Definition), StringComparer.Ordinal)
                        .ThenBy(_ => _.Key, StringComparer.Ordinal)
                        .Select(_ => new ResolvedConceptValidationRuleVariant
                        {
                            Definition = _.First().Definition,
                            Evidence = OrderedEvidence(_.Select(fact => fact.Evidence))
                        })
                        .ToArray();
                    var rule = new ResolvedConceptValidationRule
                    {
                        Concept = group.First().Definition.Concept,
                        RuleIdentity = group.First().Definition.RuleIdentity,
                        Variants = variants
                    };

                    if (rule.IsConflicted)
                    {
                        diagnostics.Add(ConflictFor(rule));
                    }

                    return rule;
                })
        ];

    static ResolvedArtifactPlacement[] ResolvePlacements(
        IEnumerable<ArtifactPlacementFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
            .GroupBy(_ => Structural.ArtifactKey(_.Artifact), StringComparer.Ordinal)
            .OrderBy(_ => Canonical.ArtifactKey(_.First().Artifact), StringComparer.Ordinal)
            .ThenBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var variants = group
                    .GroupBy(_ => Structural.Placement(_.Placement), StringComparer.Ordinal)
                    .OrderBy(_ => Canonical.Placement(_.First().Placement), StringComparer.Ordinal)
                    .ThenBy(_ => _.Key, StringComparer.Ordinal)
                    .Select(_ => new ResolvedArtifactPlacementVariant
                    {
                        Placement = _.First().Placement,
                        Evidence = OrderedEvidence(_.Select(fact => fact.Evidence))
                    })
                    .ToArray();
                var placement = new ResolvedArtifactPlacement
                {
                    Artifact = group.First().Artifact,
                    Variants = variants
                };

                if (placement.IsConflicted)
                {
                    diagnostics.Add(ConflictFor(placement));
                }

                return placement;
            })
        ];

    static ResolvedRelationship[] ResolveRelationships(
        IEnumerable<RelationshipFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
            .GroupBy(_ => Structural.RelationshipKey(_.Definition.Key), StringComparer.Ordinal)
            .OrderBy(_ => Canonical.RelationshipKey(_.First().Definition.Key), StringComparer.Ordinal)
            .ThenBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var definitions = group
                    .GroupBy(_ => Structural.Relationship(_.Definition), StringComparer.Ordinal)
                    .OrderBy(_ => Canonical.Relationship(_.First().Definition), StringComparer.Ordinal)
                    .ThenBy(_ => _.Key, StringComparer.Ordinal)
                    .Select(_ => _.First().Definition)
                    .ToArray();
                var relationship = new ResolvedRelationship
                {
                    Key = definitions[0].Key,
                    Definitions = definitions,
                    Evidence = OrderedEvidence(group.Select(_ => _.Evidence))
                };

                if (relationship.IsConflicted)
                {
                    diagnostics.Add(ConflictFor(relationship));
                }

                return relationship;
            })
        ];

    static IEnumerable<GenerationDiagnostic> InvalidConceptFactDiagnostics(IEnumerable<ConceptRepresentationFact> facts) =>
        facts
            .Where(_ => _.Subject != _.Definition.Concept)
            .Select(_ => new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.InvalidConceptFact,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Concept representation fact '{_.Id.Value}' targets '{_.Definition.Concept.Value}' but asserts subject '{_.Subject.Value}'",
                Source = _.Evidence.Source,
                Subject = _.Subject
            });

    static IEnumerable<GenerationDiagnostic> InvalidConceptFactDiagnostics(IEnumerable<ConceptAttributeFact> facts) =>
        facts
            .Where(_ => _.Subject != _.Definition.Concept)
            .Select(_ => new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.InvalidConceptFact,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Concept attribute fact '{_.Id.Value}' targets '{_.Definition.Concept.Value}' but asserts subject '{_.Subject.Value}'",
                Source = _.Evidence.Source,
                Subject = _.Subject
            });

    static IEnumerable<GenerationDiagnostic> InvalidConceptFactDiagnostics(IEnumerable<ConceptValidationRuleFact> facts) =>
        facts
            .Where(_ => _.Subject != _.Definition.Concept)
            .Select(_ => new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.InvalidConceptFact,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = $"Concept validation rule fact '{_.Id.Value}' targets '{_.Definition.Concept.Value}' but asserts subject '{_.Subject.Value}'",
                Source = _.Evidence.Source,
                Subject = _.Subject
            });

    static Evidence[] OrderedEvidence(IEnumerable<Evidence> evidence) =>
        [.. evidence
            .GroupBy(Canonical.Evidence, StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(_ => _.First())];

    static IEnumerable<GenerationDiagnostic> ConflictingFactIdentityDiagnostics(IEnumerable<GenerationFact> facts) =>
        facts
            .GroupBy(_ => _.Id.Value, StringComparer.Ordinal)
            .Select(group => new
            {
                Id = group.Key,
                Facts = group.ToArray(),
                Definitions = group.Select(FactDefinition).Distinct(StringComparer.Ordinal).ToArray()
            })
            .Where(_ => _.Definitions.Length > 1)
            .Select(_ => new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.ConflictingFactIdentity,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Conflict,
                Message = $"Fact identity '{_.Id}' was reused for {_.Definitions.Length} different semantic assertions",
                Source = FirstSource(_.Facts.Select(fact => fact.Evidence)),
                Subject = _.Facts.OrderBy(fact => fact.Subject.Value, StringComparer.Ordinal).First().Subject
            });

    static string FactDefinition(GenerationFact fact) => Structural.FactDefinition(fact);

    static GenerationDiagnostic ConflictFor(ResolvedArtifact artifact) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingArtifact,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Artifact '{artifact.Key.Subject.Value}' has {artifact.Variants.Count} incompatible {artifact.Key.Kind} definitions",
        Source = FirstSource(artifact.Variants.SelectMany(_ => _.Evidence)),
        Subject = artifact.Key.Subject
    };

    static GenerationDiagnostic ConflictFor(ResolvedConceptRepresentation representation) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingConceptRepresentation,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Concept '{representation.Concept.Value}' has {representation.Variants.Count} incompatible representations",
        Source = FirstSource(representation.Variants.SelectMany(_ => _.Evidence)),
        Subject = representation.Concept
    };

    static GenerationDiagnostic ConflictFor(ResolvedConceptAttribute attribute) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingConceptAttribute,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Concept '{attribute.Concept.Value}' has {attribute.Variants.Count} incompatible '{attribute.Name}' attribute definitions",
        Source = FirstSource(attribute.Variants.SelectMany(_ => _.Evidence)),
        Subject = attribute.Concept
    };

    static GenerationDiagnostic ConflictFor(ResolvedConceptValidationRule rule) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingConceptValidationRule,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Concept '{rule.Concept.Value}' has {rule.Variants.Count} incompatible validation definitions for rule identity '{rule.RuleIdentity}'",
        Source = FirstSource(rule.Variants.SelectMany(_ => _.Evidence)),
        Subject = rule.Concept
    };

    static GenerationDiagnostic ConflictFor(ResolvedArtifactPlacement placement) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingPlacement,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Artifact '{placement.Artifact.Subject.Value}' has {placement.EffectiveVariants.Count} equally strong incompatible {placement.Artifact.Kind} placements",
        Source = FirstSource(placement.EffectiveVariants.SelectMany(_ => _.Evidence)),
        Subject = placement.Artifact.Subject
    };

    static GenerationDiagnostic ConflictFor(ResolvedRelationship relationship) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingRelationship,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Conflict,
        Message = $"Relationship '{relationship.Key.Kind}' from '{relationship.Key.Source.Value}' to '{relationship.Key.Target.Value}' has {relationship.Definitions.Count} incompatible definitions",
        Source = FirstSource(relationship.Evidence),
        Subject = relationship.Key.Source
    };

    static SourceRange? FirstSource(IEnumerable<Evidence> evidence) =>
        evidence
            .Where(_ => _.Source is not null)
            .OrderBy(Canonical.Evidence, StringComparer.Ordinal)
            .Select(_ => _.Source)
            .FirstOrDefault();
}
