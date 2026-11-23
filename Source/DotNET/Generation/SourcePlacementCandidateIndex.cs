// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

sealed class SourcePlacementCandidateIndex(GenerationFact[] facts)
{
    readonly Dictionary<ArtifactKey, ArtifactFact[]> _declarations = GranularArtifactResolver.Resolve(facts, [], []).GroupBy(fact => fact.Definition.Key).ToDictionary(group => group.Key, group => group.ToArray());

    public bool HasUnambiguousDeclaration(ArtifactKey key) => _declarations.TryGetValue(key, out var declarations) && declarations.Length > 0 &&
        declarations.Select(fact => Structural.Artifact(fact.Definition)).Distinct(StringComparer.Ordinal).Count() == 1;

    public GenerationFact[] Inputs(ArtifactSourcePlacementCandidateFact[] candidates) => SourcePlacementCandidateDerivation.CanonicalInputs(
        candidates.Concat(candidates.SelectMany(candidate => SupportingFacts(candidate.Definition))));

    public bool HasProduction(ArtifactSourcePlacementCandidateDefinition definition) => Productions(definition).Any();

    static ArtifactKey? DeclarationKey(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Key,
        ArtifactDeclarationFact declaration => declaration.Definition.Artifact,
        ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact,
        ArtifactMemberTypeUseFact member => member.Definition.Member.Artifact,
        TypeUseBindingFact binding => binding.Definition.Member.Artifact,
        ArtifactMemberRoleFact role => role.Definition.Member.Artifact,
        _ => null
    };

    IEnumerable<GenerationFact> SupportingFacts(ArtifactSourcePlacementCandidateDefinition definition) =>
        facts.Where(fact => DeclarationKey(fact) == definition.Artifact || DeclarationKey(fact) == definition.Owner)
            .Concat(Productions(definition))
            .Concat(facts.OfType<BackendScopeFact>().Where(fact => fact.Definition.Key == definition.Artifact.Scope || fact.Definition.Key == definition.Owner.Scope));

    IEnumerable<RelationshipFact> Productions(ArtifactSourcePlacementCandidateDefinition definition) => facts.OfType<RelationshipFact>().Where(fact =>
        definition.Reason == ArtifactSourcePlacementReason.ProducingCommand && fact.Definition.Key.Kind == RelationshipKind.Produces &&
        fact.Subject == definition.Owner.Subject && fact.Definition.Key.Source == definition.Owner.Subject && fact.Definition.Key.Target == definition.Artifact.Subject &&
        fact.Definition.Key.SourceScope == definition.Owner.Scope && fact.Definition.Key.TargetScope == definition.Artifact.Scope);
}
