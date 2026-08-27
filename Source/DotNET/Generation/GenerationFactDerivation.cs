// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Derives source-neutral facts once from one fixed admitted base snapshot.
/// </summary>
public static class GenerationFactDerivation
{
    /// <summary>
    /// Runs the closed built-in derivation rule set over the admitted facts in <paramref name="snapshot"/>.
    /// </summary>
    /// <param name="snapshot">The immutable adapter run containing the admitted base facts.</param>
    /// <returns>An immutable derivation snapshot whose rules consumed only the fixed base fact array.</returns>
    public static GenerationDerivationSnapshot Derive(AdapterRunSnapshot snapshot)
    {
        var baseFacts = AdapterRunCanonicalizer.FactRecords(snapshot.Facts)
            .OrderBy(record => record.Fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(record => record.Fact.Subject.Value, StringComparer.Ordinal)
            .ThenBy(record => Structural.FactFamily(record.Fact))
            .ThenBy(record => Structural.FactDefinition(record.Fact), StringComparer.Ordinal)
            .ToImmutableArray();
        var typeUseBindings = TypeUseBindingDerivation.Derive(baseFacts);
        var derivation = new GenerationDerivationSnapshot
        {
            Rules =
            [
                new GenerationDerivationRuleRecord
                {
                    Rule = TypeUseBindingDerivation.Rule,
                    Inputs = typeUseBindings.Inputs,
                    Outputs = [.. typeUseBindings.Facts.Select(record => record.Fact.Id)],
                    Diagnostics = typeUseBindings.Diagnostics
                }
            ],
            Facts = typeUseBindings.Facts,
            Diagnostics = typeUseBindings.Diagnostics
        };

        return AdapterRunCanonicalizer.Derivation(derivation);
    }
}
