// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.for_ArtifactMemberRoleSelection.given;

public sealed class selection_result
{
    public static readonly GenerationDerivationRuleIdentity Rule = new() { Id = "cratis.screenplay.artifact-member-role-selection", Version = "1.0.0" };

    public IReadOnlyList<GenerationFactRecord> SelectedRoles { get; private init; } = [];
    public IEnumerable<ArtifactMemberRoleFact> Roles => SelectedRoles.Select(record => (ArtifactMemberRoleFact)record.Fact);
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; private init; } = [];
    public IReadOnlyList<GenerationDerivationRuleRecord> RoleSelectionRules { get; private init; } = [];
    public ImmutableDictionary<GenerationFact, GenerationFactDisposition> Dispositions { get; private init; } = [];

    public static selection_result FromContributions(IEnumerable<AdapterContribution> contributions)
    {
        var generated = new ScreenplayDefinitionGenerator().Generate(role_candidates.Capture(contributions), new() { Domain = "Ordering" });
        return new()
        {
            SelectedRoles = [.. generated.AdapterRun.Derivation!.Facts.Where(record => record.Fact is ArtifactMemberRoleFact && record.Lineage?.Producer == Rule)],
            Diagnostics = generated.Diagnostics,
            RoleSelectionRules = [.. generated.AdapterRun.Derivation.Rules.Where(rule => rule.Rule == Rule)],
            Dispositions = generated.AdapterRun.Facts.Where(record => record.Fact is ArtifactMemberRoleSelectionFact).GroupBy(record => record.Fact).ToImmutableDictionary(group => group.Key, group => group.First().Disposition)
        };
    }
}
