// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class MethodEligibilityLineage
{
    public static GenerationDerivationRuleIdentity Rule { get; } = new() { Id = "cratis.screenplay.method-fact-eligibility", Version = "1.0.0" };

    public static GenerationDerivationRuleRecord[] Rules(EffectiveGenerationInputs effective) =>
        effective.OriginalFacts.Any(fact => fact is MethodExecutionClassificationFact or MethodFactSupportFact)
            ? [new() { Rule = Rule, Inputs = [.. effective.OriginalFacts.Where(fact => fact is MethodExecutionClassificationFact or MethodFactSupportFact).Select(fact => fact.Id).Concat(effective.OriginalFacts.OfType<MethodFactSupportFact>().Select(fact => fact.Definition?.Candidate).OfType<FactId>()).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)], Outputs = [], Diagnostics = effective.Diagnostics }]
            : [];

    public static ImmutableArray<GenerationFactRecord> Apply(IEnumerable<GenerationFactRecord> records, EffectiveGenerationInputs effective)
    {
        return [.. records.Select(record => record.Lineage is null ? record : record with
        {
            Lineage = record.Lineage with
            {
                Inputs = [.. record.Lineage.Inputs.Concat(record.Lineage.Inputs.SelectMany(input => effective.EligibilityInputs.GetValueOrDefault(input, []).Select(fact => fact.Id))).Distinct().OrderBy(id => id.Value, StringComparer.Ordinal)],
                Evidence = [.. record.Lineage.Evidence.Concat(record.Lineage.Inputs.SelectMany(input => effective.EligibilityInputs.GetValueOrDefault(input, [])).Select(fact => fact.Evidence)).Distinct().OrderBy(Structural.Evidence, StringComparer.Ordinal)]
            }
        })];
    }
}
