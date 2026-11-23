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

    public static ImmutableArray<GenerationFactRecord> Apply(IEnumerable<GenerationFactRecord> records, EffectiveGenerationInputs effective) =>
        effective.EligibilityInputs.IsEmpty ? [.. records] : [.. records.Select(record => AddDependencies(record, effective))];

    static GenerationFactRecord AddDependencies(GenerationFactRecord record, EffectiveGenerationInputs effective)
    {
        if (record.Lineage is not { } lineage)
        {
            return record;
        }

        var additions = lineage.Inputs.SelectMany(input => effective.EligibilityInputs.GetValueOrDefault(input, []))
            .Where(fact => !lineage.Inputs.Contains(fact.Id))
            .OrderBy(Structural.Fact, StringComparer.Ordinal)
            .DistinctBy(fact => fact.Id).ToArray();
        if (additions.Length == 0)
        {
            return record;
        }

        var dependencies = lineage.Inputs.Zip(lineage.Evidence, (id, evidence) => (Id: id, Evidence: evidence))
            .Concat(additions.Select(fact => (fact.Id, fact.Evidence)))
            .OrderBy(dependency => dependency.Id.Value, StringComparer.Ordinal).ToArray();
        return record with
        {
            Lineage = lineage with
            {
                Inputs = [.. dependencies.Select(dependency => dependency.Id)],
                Evidence = [.. dependencies.Select(dependency => dependency.Evidence)]
            }
        };
    }
}
