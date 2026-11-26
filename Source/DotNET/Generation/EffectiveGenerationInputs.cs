// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

sealed record EffectiveGenerationInputs(
    ImmutableArray<GenerationFact> OriginalFacts,
    ImmutableArray<GenerationFact> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics,
    ImmutableDictionary<GenerationFactInstance, GenerationFactDisposition> Dispositions,
    ImmutableDictionary<FactId, ImmutableArray<GenerationFact>> EligibilityInputs)
{
    public ImmutableArray<GenerationFactRecord> NameYieldFallbacks { get; init; } = [];

    public ImmutableDictionary<GenerationFactInstance, ImmutableArray<GenerationDiagnostic>> NameYieldFacetExplanations { get; init; } = [];

    public ImmutableArray<GenerationDerivationRuleRecord> NameYieldRules { get; init; } = [];

    public ArtifactMemberRoleSelectionState RoleSelection { get; init; } = new();

    public IEnumerable<GenerationFact> BindingFacts => Facts.Concat(NameYieldFallbacks.Select(record => record.Fact)).Concat(RoleSelection.Roles.Select(record => record.Fact));

    public IEnumerable<GenerationFact> OmittedFacts => OriginalFacts.Where(HasDisposition);

    public bool HasDisposition(GenerationFact fact) => Dispositions.ContainsKey(GenerationFactInstance.Of(fact));

    public GenerationFactDisposition DispositionOf(GenerationFact fact) => Dispositions[GenerationFactInstance.Of(fact)];

    public static EffectiveGenerationInputs FromFacts(IEnumerable<GenerationFact> facts) => ArtifactMemberRoleSelection.Select(ConceptNameYieldSelection.Select(MethodFactEligibility.Select([.. facts])));

    public static EffectiveGenerationInputs FromContributions(IEnumerable<AdapterContribution> contributions)
    {
        var input = contributions.ToArray();
        var invalid = input.SelectMany(contribution => InvalidSupports(contribution.Facts)).ToHashSet();
        return ArtifactMemberRoleSelection.Select(ConceptNameYieldSelection.Select(MethodFactEligibility.Select([.. input.SelectMany(contribution => contribution.Facts)], invalid)));
    }

    static IEnumerable<MethodFactSupportFact> InvalidSupports(IReadOnlyList<GenerationFact> facts) =>
        facts.OfType<MethodFactSupportFact>().Where(support => !MethodFactEligibility.HasCandidate(support, facts));
}
