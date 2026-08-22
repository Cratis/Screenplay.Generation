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
        var facts = orderedContributions.SelectMany(_ => _.Facts).ToArray();
        var diagnostics = orderedContributions.SelectMany(_ => _.Diagnostics).ToList();

        diagnostics.AddRange(ConflictingFactIdentityDiagnostics(facts));

        var artifacts = ResolveArtifacts(facts.OfType<ArtifactFact>(), diagnostics);
        var conceptRepresentationFacts = facts.OfType<ConceptRepresentationFact>().ToArray();
        diagnostics.AddRange(InvalidConceptFactDiagnostics(conceptRepresentationFacts));
        var conceptRepresentations = ResolveConceptRepresentations(
            conceptRepresentationFacts.Where(_ => _.Subject == _.Definition.Concept),
            diagnostics);
        var placements = ResolvePlacements(facts.OfType<ArtifactPlacementFact>(), diagnostics);
        var relationships = ResolveRelationships(facts.OfType<RelationshipFact>(), diagnostics);

        return new()
        {
            Artifacts = artifacts,
            ConceptRepresentations = conceptRepresentations,
            Placements = placements,
            Relationships = relationships,
            Diagnostics = [.. diagnostics.OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)]
        };
    }

    static ResolvedArtifact[] ResolveArtifacts(
        IEnumerable<ArtifactFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
            .GroupBy(_ => Canonical.ArtifactKey(_.Definition.Key), StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var variants = group
                    .GroupBy(_ => Canonical.Artifact(_.Definition), StringComparer.Ordinal)
                    .OrderBy(_ => _.Key, StringComparer.Ordinal)
                    .Select(_ => new ResolvedArtifactVariant
                    {
                        Definition = _.First().Definition,
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
                        .GroupBy(_ => Canonical.ConceptRepresentation(_.Definition), StringComparer.Ordinal)
                        .OrderBy(_ => _.Key, StringComparer.Ordinal)
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

    static ResolvedArtifactPlacement[] ResolvePlacements(
        IEnumerable<ArtifactPlacementFact> facts,
        List<GenerationDiagnostic> diagnostics) =>
        [
            .. facts
            .GroupBy(_ => Canonical.ArtifactKey(_.Artifact), StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var variants = group
                    .GroupBy(_ => Canonical.Placement(_.Placement), StringComparer.Ordinal)
                    .OrderBy(_ => _.Key, StringComparer.Ordinal)
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
            .GroupBy(_ => Canonical.RelationshipKey(_.Definition.Key), StringComparer.Ordinal)
            .OrderBy(_ => _.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var definitions = group
                    .GroupBy(_ => Canonical.Relationship(_.Definition), StringComparer.Ordinal)
                    .OrderBy(_ => _.Key, StringComparer.Ordinal)
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
                Message = $"Concept representation fact '{_.Id.Value}' targets '{_.Definition.Concept.Value}' but asserts subject '{_.Subject.Value}'",
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
                Message = $"Fact identity '{_.Id}' was reused for {_.Definitions.Length} different semantic assertions",
                Source = FirstSource(_.Facts.Select(fact => fact.Evidence)),
                Subject = _.Facts.OrderBy(fact => fact.Subject.Value, StringComparer.Ordinal).First().Subject
            });

    static string FactDefinition(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => $"artifact:{Canonical.Artifact(artifact.Definition)}",
        ConceptRepresentationFact representation => $"concept-representation:{Canonical.ConceptRepresentation(representation.Definition)}",
        ArtifactPlacementFact placement => $"placement:{Canonical.ArtifactKey(placement.Artifact)}:{Canonical.Placement(placement.Placement)}",
        RelationshipFact relationship => $"relationship:{Canonical.Relationship(relationship.Definition)}",
        _ => fact.GetType().FullName ?? fact.GetType().Name
    };

    static GenerationDiagnostic ConflictFor(ResolvedArtifact artifact) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingArtifact,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = $"Artifact '{artifact.Key.Subject.Value}' has {artifact.Variants.Count} incompatible {artifact.Key.Kind} definitions",
        Source = FirstSource(artifact.Variants.SelectMany(_ => _.Evidence)),
        Subject = artifact.Key.Subject
    };

    static GenerationDiagnostic ConflictFor(ResolvedConceptRepresentation representation) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingConceptRepresentation,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = $"Concept '{representation.Concept.Value}' has {representation.Variants.Count} incompatible representations",
        Source = FirstSource(representation.Variants.SelectMany(_ => _.Evidence)),
        Subject = representation.Concept
    };

    static GenerationDiagnostic ConflictFor(ResolvedArtifactPlacement placement) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingPlacement,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = $"Artifact '{placement.Artifact.Subject.Value}' has {placement.EffectiveVariants.Count} equally strong incompatible {placement.Artifact.Kind} placements",
        Source = FirstSource(placement.EffectiveVariants.SelectMany(_ => _.Evidence)),
        Subject = placement.Artifact.Subject
    };

    static GenerationDiagnostic ConflictFor(ResolvedRelationship relationship) => new()
    {
        Code = GenerationDiagnosticCodes.ConflictingRelationship,
        Severity = GenerationDiagnosticSeverity.Error,
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
