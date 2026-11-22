// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class BackendScopeFactReferences
{
    public static IReadOnlyList<BackendScopeKey> Of(GenerationFact fact) =>
    [
        .. Scopes(fact).Where(scope => scope is not null).Cast<BackendScopeKey>().Distinct()
            .OrderBy(Structural.BackendScopeKey, StringComparer.Ordinal)
    ];

    public static ArtifactKey? Owner(GenerationFact fact) => fact switch
    {
        ArtifactFact artifact => artifact.Definition.Key,
        ArtifactDeclarationFact declaration => declaration.Definition.Artifact,
        ArtifactPlacementFact placement => placement.Artifact,
        _ => MemberOwner(fact)
    };

    static ArtifactKey? MemberOwner(GenerationFact fact) => fact switch
    {
        ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member.Artifact,
        TypeUseBindingFact binding => binding.Definition.Member.Artifact,
        ArtifactMemberRoleFact role => role.Definition.Member.Artifact,
        _ => null
    };

    static IEnumerable<BackendScopeKey?> Scopes(GenerationFact fact) => fact switch
    {
        BackendScopeFact scope => [scope.Definition.Key],
        ArtifactFact artifact => artifact.Definition.Properties.Select(property => property.Type.Scope).Prepend(artifact.Definition.Key.Scope),
        ArtifactMemberTypeUseFact typeUse => [typeUse.Definition.Member.Artifact.Scope, typeUse.Definition.Type.ObservedScope],
        TypeUseBindingFact binding => [binding.Definition.Member.Artifact.Scope, binding.Definition.Target.Scope],
        RelationshipFact relationship => [relationship.Definition.Key.SourceScope, relationship.Definition.Key.TargetScope],
        _ => UsageScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> UsageScopes(GenerationFact fact) => fact switch
    {
        ArtifactUsageDeclarationFact declaration => [declaration.Definition.Artifact.Scope],
        EntryPointFact entry => [entry.Definition.Artifact.Scope],
        MethodExecutionClassificationFact classification => [classification.Definition.Scope],
        MethodFactSupportFact support => support.Definition.Alternatives.Select(alternative => alternative.Scope),
        DeclarationInputMemberDeclarationFact member => [member.Definition.Member.Scope],
        DeclarationInputMemberTypeUseFact typeUse => [typeUse.Definition.Member.Scope, typeUse.Definition.Type.ObservedScope],
        EntryPointInputTransferFact transfer => [transfer.Definition.Artifact.Scope],
        MethodArtifactUsageFact usage => [usage.Definition.SourceScope, usage.Definition.TargetScope],
        SpecificationScenarioFact scenario => [scenario.Definition.TargetArtifact.Scope],
        SpecificationStepFact step => [step.Definition.Artifact?.Scope],
        SpecificationValueFact value => [value.Definition.Type?.Scope],
        _ => [Owner(fact)?.Scope]
    };
}
