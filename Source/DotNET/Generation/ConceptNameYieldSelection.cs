// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class ConceptNameYieldSelection
{
    static readonly GenerationDerivationRuleIdentity _rule = new() { Id = "cratis.screenplay.concept-name-yield", Version = "1.0.0" };

    public static EffectiveGenerationInputs Select(EffectiveGenerationInputs input)
    {
        var concepts = input.Facts.Select(Concept).OfType<ConceptDeclaration>().ToArray();
        if (!concepts.Any(concept => IsYielding(concept.Fact)))
        {
            return input;
        }

        var reservations = ConceptNameReservations.Find(input.Facts);
        var conflictNames = concepts.GroupBy(concept => concept.Name, StringComparer.Ordinal)
            .Where(group => group.Select(concept => concept.Key.Subject).Distinct().Count() > 1)
            .Select(group => group.Key).Concat(reservations.Select(reservation => reservation.Name)).ToHashSet(StringComparer.Ordinal);
        var conflicts = concepts.Where(concept => conflictNames.Contains(concept.Name)).ToArray();
        var yielded = conflicts.Where(concept => ShouldYield(concept, concepts)).ToArray();
        if (yielded.Length == 0)
        {
            return input;
        }

        var dropped = yielded.Select(concept => GenerationFactInstance.Of(concept.Fact)).ToHashSet();
        var facts = input.Facts.Where(fact => !dropped.Contains(GenerationFactInstance.Of(fact))).ToImmutableArray();
        var diagnostics = yielded.Select(concept => Diagnostic(concept, input.Facts)).ToImmutableArray();
        var conflictInputs = conflicts.Select(concept => new ConceptNameReservations.Reservation(concept.Name, concept.Fact))
            .Concat(reservations).ToArray();
        var fallbacks = Fallbacks(facts, yielded, conflictInputs, diagnostics);
        var dispositions = input.Dispositions.ToBuilder();
        foreach (var concept in yielded)
        {
            dispositions[GenerationFactInstance.Of(concept.Fact)] = GenerationFactDisposition.OmittedWithDiagnostic;
        }

        return input with
        {
            Facts = facts,
            Diagnostics = [.. input.Diagnostics, .. diagnostics],
            Dispositions = dispositions.ToImmutable(),
            NameYieldFallbacks = fallbacks,
            NameYieldRules =
            [
                new GenerationDerivationRuleRecord
                {
                    Rule = _rule,
                    Inputs = [.. conflictInputs.Select(source => source.Fact.Id).Concat(fallbacks.SelectMany(record => record.Lineage!.Inputs)).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                    Outputs = [.. fallbacks.Select(record => record.Fact.Id)],
                    Diagnostics = diagnostics
                }
            ]
        };
    }

    static bool ShouldYield(ConceptDeclaration concept, ConceptDeclaration[] concepts) =>
        IsYielding(concept.Fact) && !HasStructuralConflict(concept, concepts) &&
        !concepts.Any(other => other.Key == concept.Key && !IsYielding(other.Fact));

    static bool HasStructuralConflict(ConceptDeclaration concept, ConceptDeclaration[] concepts) =>
        concepts.Any(other => other.Key == concept.Key && (other.Name != concept.Name || other.Description != concept.Description));

    static bool IsYielding(GenerationFact fact) => fact is ArtifactDeclarationFact
    {
        Definition.NameConflictYield.Diagnostic: { } diagnostic
    } declaration && diagnostic.Outcome is GenerationDiagnosticOutcome.Conflict or GenerationDiagnosticOutcome.Unsupported &&
        diagnostic.Subject == declaration.Subject && diagnostic.Facts.Contains(declaration.Id);

    static ConceptDeclaration? Concept(GenerationFact fact) => fact switch
    {
        ArtifactDeclarationFact declaration when declaration.Definition.Artifact.Kind == ArtifactKind.Concept =>
            new(declaration.Definition.Artifact, declaration.Definition.Name, declaration.Definition.Description, fact),
        ArtifactFact artifact when artifact.Definition.Key.Kind == ArtifactKind.Concept =>
            new(artifact.Definition.Key, artifact.Definition.Name, artifact.Definition.Description, fact),
        _ => null
    };

    static GenerationDiagnostic Diagnostic(ConceptDeclaration concept, ImmutableArray<GenerationFact> facts)
    {
        var marker = ((ArtifactDeclarationFact)concept.Fact).Definition.NameConflictYield!;
        return marker.Diagnostic with
        {
            Facts = [.. marker.Diagnostic.Facts.Concat(facts.Where(fact => IsConceptFacet(fact, concept)).Select(fact => fact.Id)).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)]
        };
    }

    static bool IsConceptFacet(GenerationFact fact, ConceptDeclaration concept) =>
        fact.Subject == concept.Key.Subject && fact.Evidence.Adapter == concept.Fact.Evidence.Adapter &&
        fact is ConceptRepresentationFact or ConceptAttributeFact or ConceptValidationRuleFact;

    static ImmutableArray<GenerationFactRecord> Fallbacks(
        ImmutableArray<GenerationFact> facts,
        ConceptDeclaration[] yielded,
        ConceptNameReservations.Reservation[] conflictInputs,
        ImmutableArray<GenerationDiagnostic> diagnostics)
    {
        var retained = facts.Select(Concept).OfType<ConceptDeclaration>().Select(concept => concept.Key).ToHashSet();
        return
        [
            .. facts.OfType<ArtifactMemberTypeUseFact>()
                .OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
                .SelectMany(typeUse => yielded.Select((concept, index) => (Concept: concept, Diagnostic: diagnostics[index]))
                    .Where(pair => Matches(typeUse, pair.Concept, retained))
                    .Select(pair => Fallback(typeUse, pair.Concept, pair.Diagnostic, conflictInputs)))
        ];
    }

    static bool Matches(ArtifactMemberTypeUseFact typeUse, ConceptDeclaration concept, HashSet<ArtifactKey> retained) =>
        typeUse.Definition.Type.ObservedTypeSubject == concept.Key.Subject &&
        typeUse.Definition.Type.ObservedScope == concept.Key.Scope && !retained.Contains(concept.Key);

    static GenerationFactRecord Fallback(ArtifactMemberTypeUseFact typeUse, ConceptDeclaration concept, GenerationDiagnostic diagnostic, ConceptNameReservations.Reservation[] conflictInputs)
    {
        var inputs = conflictInputs.Where(candidate => candidate.Name == concept.Name).Select(candidate => candidate.Fact)
            .Append(typeUse).DistinctBy(fact => fact.Id).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ToArray();
        return new GenerationFactRecord
        {
            Fact = new TypeUseTargetOmittedFact
            {
                Id = new FactId { Value = $"generation:derive:concept-name-yield:1:{Uri.EscapeDataString(concept.Fact.Id.Value)}:{Uri.EscapeDataString(typeUse.Id.Value)}" },
                Subject = typeUse.Subject,
                Evidence = typeUse.Evidence,
                Definition = new TypeUseTargetOmittedDefinition { Member = typeUse.Definition.Member, Diagnostic = diagnostic }
            },
            Lineage = new GenerationFactLineage
            {
                Producer = _rule,
                Inputs = [.. inputs.Select(fact => fact.Id)],
                Evidence = [.. inputs.Select(fact => fact.Evidence)]
            }
        };
    }

    sealed record ConceptDeclaration(ArtifactKey Key, string Name, string? Description, GenerationFact Fact);
}
