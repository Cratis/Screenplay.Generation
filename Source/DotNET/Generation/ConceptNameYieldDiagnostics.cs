// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class ConceptNameYieldDiagnostics
{
    public static ImmutableDictionary<string, GenerationDiagnostic> Coalesce(
        IEnumerable<ArtifactDeclarationFact> declarations,
        EffectiveGenerationInputs effective,
        GenerationDerivationRuleIdentity rule) => declarations
            .GroupBy(Key, StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => Merge(group, effective, rule), StringComparer.Ordinal);

    public static string Key(ArtifactDeclarationFact declaration) => Structural.SemanticKey(
        "concept-name-yield-diagnostic",
        Structural.ArtifactKey(declaration.Definition.Artifact),
        Structural.Diagnostic(declaration.Definition.NameConflictYield!.Diagnostic with { Facts = [], Lineage = null }));

    public static ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>> FacetExplanations(
        IEnumerable<ArtifactDeclarationFact> declarations,
        IEnumerable<GenerationFact> facts,
        ImmutableDictionary<string, GenerationDiagnostic> coalesced) => declarations
            .SelectMany(declaration => facts.Where(fact => IsConceptFacet(fact, declaration)).Select(fact => (Instance: GenerationFactInstance.Of(fact), Diagnostic: coalesced[Key(declaration)])))
            .GroupBy(pair => pair.Instance)
            .ToImmutableDictionary(
                group => group.Key,
                group => group.Select(pair => pair.Diagnostic).DistinctBy(Canonical.Diagnostic).OrderBy(Canonical.Diagnostic, StringComparer.Ordinal).ToImmutableArray());

    static GenerationDiagnostic Merge(IEnumerable<ArtifactDeclarationFact> input, EffectiveGenerationInputs effective, GenerationDerivationRuleIdentity rule)
    {
        var declarations = input.OrderBy(Structural.Fact, StringComparer.Ordinal).ToArray();
        var facts = declarations.SelectMany(declaration => declaration.Definition.NameConflictYield!.Diagnostic.Facts
            .Concat(effective.Facts.Where(fact => IsConceptFacet(fact, declaration)).Select(fact => fact.Id)))
            .Distinct().OrderBy(id => id.Value, StringComparer.Ordinal).ToImmutableArray();
        return declarations[0].Definition.NameConflictYield!.Diagnostic with
        {
            Facts = facts,
            Lineage = DiagnosticIntentLineage.Create(rule, effective.Facts.Where(fact => facts.Contains(fact.Id)), effective)
        };
    }

    static bool IsConceptFacet(GenerationFact fact, ArtifactDeclarationFact declaration) =>
        fact.Subject == declaration.Definition.Artifact.Subject && fact.Evidence.Adapter == declaration.Evidence.Adapter &&
        fact is ConceptRepresentationFact or ConceptAttributeFact or ConceptValidationRuleFact;
}
