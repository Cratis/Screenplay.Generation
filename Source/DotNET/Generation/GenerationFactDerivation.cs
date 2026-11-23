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

    /// <summary>Derives facts with a host-owned fixed source placement resolver.</summary>
    /// <param name="snapshot">The fixed admitted base snapshot.</param>
    /// <param name="resolver">The fixed source mapper.</param>
    /// <returns>The canonical derived facts and lineage.</returns>
    public static GenerationDerivationSnapshot Derive(AdapterRunSnapshot snapshot, IArtifactSourcePlacementResolver resolver) =>
        Derive(EffectiveGenerationInputs.FromFacts(snapshot.Facts.Select(record => record.Fact)), resolver);

    internal static GenerationDerivationSnapshot Derive(EffectiveGenerationInputs effective, IArtifactSourcePlacementResolver? sourceResolver = null)
    {
        var canonicalInput = AdapterRunCanonicalizer.FactRecords(effective.Facts.Select(fact => new GenerationFactRecord { Fact = fact }));
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
        var sourcePlacements = SourcePlacementCandidateDerivation.Derive(baseFacts, sourceResolver);
        ImmutableArray<GenerationDerivationRuleRecord> sourceRules = sourcePlacements.Inputs.IsEmpty ? [] :
        [
            new GenerationDerivationRuleRecord { Rule = SourcePlacementCandidateDerivation.Rule, Inputs = sourcePlacements.Inputs, Outputs = [.. sourcePlacements.Facts.Select(record => record.Fact.Id)], Diagnostics = sourcePlacements.Diagnostics }
        ];
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
            .Concat(sourcePlacements.Diagnostics)
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
                .. sourceRules,
                .. MethodEligibilityLineage.Rules(effective)
            ],
            Facts = MethodEligibilityLineage.Apply([.. typeUseBindings.Facts, .. artifactUsages.Facts, .. inputMembers.Facts, .. sourcePlacements.Facts], effective),
            Diagnostics = diagnostics
        };

        return AdapterRunCanonicalizer.Derivation(derivation);
    }
}
