// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

sealed record EffectiveGenerationInputs(
    ImmutableArray<GenerationFact> OriginalFacts,
    ImmutableArray<GenerationFact> Facts,
    ImmutableArray<GenerationDiagnostic> Diagnostics,
    ImmutableDictionary<FactId, GenerationFactDisposition> Dispositions,
    ImmutableDictionary<FactId, ImmutableArray<GenerationFact>> EligibilityInputs)
{
    public static EffectiveGenerationInputs FromFacts(IEnumerable<GenerationFact> facts) => MethodFactEligibility.Select([.. facts]);

    public static EffectiveGenerationInputs FromContributions(IEnumerable<AdapterContribution> contributions)
    {
        var input = contributions.ToArray();
        var invalid = input.SelectMany(contribution => InvalidSupports(contribution.Facts)).ToHashSet();
        return MethodFactEligibility.Select([.. input.SelectMany(contribution => contribution.Facts)], invalid);
    }

    static IEnumerable<MethodFactSupportFact> InvalidSupports(IReadOnlyList<GenerationFact> facts) =>
        facts.OfType<MethodFactSupportFact>().Where(support => !MethodFactEligibility.HasCandidate(support, facts));
}
