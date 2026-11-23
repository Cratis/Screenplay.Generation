// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DiagnosticIntentLineage
{
    public static GenerationFactLineage Create(GenerationDerivationRuleIdentity rule, IEnumerable<GenerationFact> inputs, EffectiveGenerationInputs effective)
    {
        var facts = inputs.ToArray();
        var dependencies = facts.Concat(facts.SelectMany(fact => effective.EligibilityInputs.GetValueOrDefault(fact.Id, [])))
            .DistinctBy(Structural.Fact).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ThenBy(Structural.Fact, StringComparer.Ordinal).ToArray();
        return new()
        {
            Producer = rule,
            Inputs = [.. dependencies.Select(fact => fact.Id)],
            Evidence = [.. dependencies.Select(fact => fact.Evidence)]
        };
    }

    public static IEnumerable<GenerationFact> LocalInputs(GenerationFact intent, GenerationDiagnostic diagnostic, IEnumerable<GenerationFact> facts) =>
        facts.Where(fact => fact.Evidence.Adapter == intent.Evidence.Adapter && diagnostic.Facts.Contains(fact.Id)).Prepend(intent);

    public static bool LocalSupportsExist(GenerationFact intent, GenerationDiagnostic diagnostic, IEnumerable<GenerationFact> facts) =>
        diagnostic.Facts.All(id => facts.Any(fact => fact.Id == id && fact.Evidence.Adapter == intent.Evidence.Adapter));
}
