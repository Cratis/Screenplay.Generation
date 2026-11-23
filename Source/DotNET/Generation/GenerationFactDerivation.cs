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
    public static GenerationDerivationSnapshot Derive(AdapterRunSnapshot snapshot) =>
        Derive(EffectiveGenerationInputs.FromFacts(snapshot.Facts.Select(record => record.Fact)));

    internal static GenerationDerivationSnapshot Derive(EffectiveGenerationInputs effective)
    {
        var canonicalInput = AdapterRunCanonicalizer.FactRecords(effective.BindingFacts.Select(fact => new GenerationFactRecord { Fact = fact }));
        var discriminatorValidation = GenerationFactDiscriminatorValidator.Validate(
            canonicalInput.Select(record => record.Fact));
        var validationDiagnostics = discriminatorValidation.Diagnostics.Concat(effective.Diagnostics).ToList();
        var validFacts = GranularArtifactResolver.ValidFactsForDerivation(
            discriminatorValidation.Facts,
            validationDiagnostics);
        validFacts = BackendScopeFactResolver.ValidFacts([.. validFacts], validationDiagnostics);
        var baseFacts = validFacts
            .Select(fact => new GenerationFactRecord { Fact = fact })
            .OrderBy(record => record.Fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(record => record.Fact.Subject.Value, StringComparer.Ordinal)
            .ThenBy(record => Structural.FactFamily(record.Fact))
            .ThenBy(record => Structural.FactDefinition(record.Fact), StringComparer.Ordinal)
            .ToImmutableArray();
        var typeUseBindings = TypeUseBindingDerivation.Derive(baseFacts);
        var artifactUsages = ArtifactUsageDerivation.Derive(baseFacts);
        var inputMembers = EntryPointInputMemberDerivation.Derive(baseFacts);
        ImmutableArray<GenerationDerivationRuleRecord> inputMemberRules = inputMembers.Inputs.IsEmpty ? [] :
        [
            new GenerationDerivationRuleRecord
            {
                Rule = EntryPointInputMemberDerivation.Rule,
                Inputs = inputMembers.Inputs,
                Outputs = [.. inputMembers.Facts.Select(record => record.Fact.Id)],
                Diagnostics = inputMembers.Diagnostics
            }
        ];
        var diagnostics = validationDiagnostics
            .Concat(typeUseBindings.Diagnostics)
            .Concat(artifactUsages.Diagnostics)
            .Concat(inputMembers.Diagnostics)
            .OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .ToImmutableArray();
        ImmutableArray<GenerationDerivationRuleRecord> usageRules = baseFacts.Any(record => record.Fact is ArtifactUsageDeclarationFact or MethodArtifactUsageFact or EntryPointFact)
            ?
            [
                new GenerationDerivationRuleRecord
                {
                    Rule = ArtifactUsageDerivation.Rule,
                    Inputs = artifactUsages.Inputs,
                    Outputs = [.. artifactUsages.Facts.Select(record => record.Fact.Id)],
                    Diagnostics = artifactUsages.Diagnostics
                }
            ]
            : [];
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
                },
                .. usageRules,
                .. inputMemberRules,
                .. MethodEligibilityLineage.Rules(effective),
                .. effective.NameYieldRules
            ],
            Facts = MethodEligibilityLineage.Apply([.. typeUseBindings.Facts, .. artifactUsages.Facts, .. inputMembers.Facts, .. effective.NameYieldFallbacks], effective),
            Diagnostics = diagnostics
        };

        return AdapterRunCanonicalizer.Derivation(derivation);
    }
}
