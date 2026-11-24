// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionLineage
{
    public static GenerationFactLineage Create(ArtifactMemberRoleSelectionFact[] candidates, EffectiveGenerationInputs input)
    {
        var dependencies = candidates.SelectMany(candidate => Dependencies(candidate, input));
        var scopes = input.OriginalFacts.OfType<BackendScopeFact>().Where(scope => candidates.Select(candidate => candidate.Definition?.Artifact?.Scope).Contains(scope.Definition.Key));
        var facts = dependencies.Concat(scopes).DistinctBy(Structural.Fact).OrderBy(fact => fact.Id.Value, StringComparer.Ordinal).ThenBy(Structural.Fact, StringComparer.Ordinal).ToArray();
        return new() { Producer = ArtifactMemberRoleSelection.Rule, Inputs = [.. facts.Select(fact => fact.Id)], Evidence = [.. facts.Select(fact => fact.Evidence)] };
    }

    static IEnumerable<GenerationFact> Dependencies(ArtifactMemberRoleSelectionFact candidate, EffectiveGenerationInputs input)
    {
        var diagnostics = (candidate.Definition?.Diagnostics ?? []).Concat(input.RoleSelection.Diagnostics.GetValueOrDefault(GenerationFactInstance.Of(candidate), []));
        var references = diagnostics.Where(diagnostic => diagnostic?.Facts.IsDefault == false).SelectMany(diagnostic => diagnostic.Facts).ToHashSet();
        return new GenerationFact[] { candidate }.Concat(input.RoleSelection.Inputs.GetValueOrDefault(GenerationFactInstance.Of(candidate), []))
            .Concat(ArtifactMemberRoleSelectionSupport.InvalidDependencies(candidate, input))
            .Concat(ArtifactMemberRoleSelectionMixing.Assertions(candidate, input.Facts))
            .Concat(input.OriginalFacts.Where(fact => fact.Evidence.Adapter == candidate.Evidence.Adapter && references.Contains(fact.Id)));
    }
}
