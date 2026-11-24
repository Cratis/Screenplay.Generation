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
        ArtifactSourcePlacementCandidateFact candidate => candidate.Definition.Artifact,
        _ => MemberOwner(fact)
    };

    static ArtifactKey? MemberOwner(GenerationFact fact) => fact switch
    {
        ArtifactMemberDeclarationFact member => member.Definition.Member.Artifact,
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Member.Artifact,
        TypeUseTargetOmittedFact omission => omission.Definition.Member.Artifact,
        TypeUseBindingFact binding => binding.Definition.Member.Artifact,
        ArtifactMemberRoleFact role => role.Definition.Member.Artifact,
        ArtifactMemberRoleSelectionFact selection => selection.Definition?.Artifact,
        _ => null
    };

    static IEnumerable<BackendScopeKey?> Scopes(GenerationFact fact) => fact switch
    {
        BackendScopeFact scope => [scope.Definition.Key],
        ArtifactSourcePlacementCandidateFact candidate => [candidate.Definition.Artifact.Scope, candidate.Definition.Owner.Scope],
        ArtifactFact artifact => artifact.Definition.Properties.Select(property => property.Type.Scope).Prepend(artifact.Definition.Key.Scope),
        _ => TypeUseScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> TypeUseScopes(GenerationFact fact) => fact switch
    {
        ArtifactMemberTypeUseFact typeUse => TypeScopes(typeUse.Definition.Type).Prepend(typeUse.Definition.Member.Artifact.Scope),
        TypeUseCompatibilityProjectionFact projection => TypeScopes(projection.Definition?.Type),
        TypeUseBindingFact binding => [binding.Definition.Member.Artifact.Scope, binding.Definition.Target.Scope],
        RelationshipFact relationship => [relationship.Definition.Key.SourceScope, relationship.Definition.Key.TargetScope],
        _ => UsageScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> UsageScopes(GenerationFact fact) => fact switch
    {
        ArtifactUsageDeclarationFact declaration => [declaration.Definition.Artifact.Scope],
        EntryPointFact entry => [entry.Definition.Artifact.Scope],
        _ => DiagnosticScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> DiagnosticScopes(GenerationFact fact) => fact switch
    {
        MethodDiagnosticProjectionFact projection => [projection.Definition.Scope],
        OccurrenceOmissionDiagnosticFact candidate => [candidate.Definition?.Occurrence?.Scope],
        ArtifactMemberRoleSelectionFact selection => [selection.Definition?.Artifact?.Scope],
        _ => ObservationScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> ObservationScopes(GenerationFact fact) => fact switch
    {
        MethodExecutionClassificationFact classification => [classification.Definition.Scope],
        MethodFactSupportFact support => support.Definition.Alternatives.Select(alternative => alternative.Scope),
        _ => InputScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> InputScopes(GenerationFact fact) => fact switch
    {
        DeclarationInputMemberTargetOmittedFact omission => [omission.Definition.Member.Scope],
        DeclarationInputMemberDeclarationFact member => [member.Definition.Member.Scope],
        DeclarationInputMemberTypeUseFact typeUse => TypeScopes(typeUse.Definition.Type).Prepend(typeUse.Definition.Member.Scope),
        EntryPointInputTransferFact transfer => [transfer.Definition.Artifact.Scope],
        _ => ExecutionScopes(fact)
    };

    static IEnumerable<BackendScopeKey?> TypeScopes(TypeUseDefinition? type) =>
        (type?.Nodes ?? []).Select(node => node?.Scope).Prepend(type?.ObservedScope);

    static IEnumerable<BackendScopeKey?> ExecutionScopes(GenerationFact fact) => fact switch
    {
        MethodArtifactUsageFact usage => [usage.Definition.SourceScope, usage.Definition.TargetScope],
        SpecificationScenarioFact scenario => [scenario.Definition.TargetArtifact.Scope],
        SpecificationStepFact step => [step.Definition.Artifact?.Scope],
        SpecificationValueFact value => [value.Definition.Type?.Scope],
        _ => [Owner(fact)?.Scope]
    };
}
