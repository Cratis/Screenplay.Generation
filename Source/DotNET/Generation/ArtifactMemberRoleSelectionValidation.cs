// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionValidation
{
    public static bool Key(ArtifactMemberRoleSelectionFact fact) => fact.Definition is { Artifact: { } artifact } definition &&
        DiagnosticIntentValidation.Subject(artifact.Subject) && Supported(artifact.Kind, ArtifactKind.Unknown) &&
        DiagnosticIntentValidation.Scope(artifact.Scope) && Supported(definition.Role, ArtifactMemberRoleKind.Unknown);

    public static bool Valid(ArtifactMemberRoleSelectionFact fact, EffectiveGenerationInputs input)
    {
        if (!Key(fact))
        {
            return false;
        }

        var definition = fact.Definition;
        return definition.Artifact.Subject == fact.Subject && definition.ObservationOrder >= 0 && Member(definition) &&
            KnownScope(definition.Artifact, input) && Diagnostics(fact, input) && Supports(fact, input) && LocalIdentity(fact, input);
    }

    static bool Member(ArtifactMemberRoleSelectionDefinition definition) => definition.Member is null ||
        (definition.Member.Artifact == definition.Artifact && DiagnosticIntentValidation.Text(definition.Member.Name));

    static bool Diagnostics(ArtifactMemberRoleSelectionFact fact, EffectiveGenerationInputs input) =>
        fact.Definition.Diagnostics?.All(diagnostic => ArtifactMemberRoleSelectionDiagnosticValidation.Valid(fact, diagnostic, input.OriginalFacts, true)) == true &&
        input.SelectionDiagnostics.GetValueOrDefault(GenerationFactInstance.Of(fact), []).All(diagnostic => ArtifactMemberRoleSelectionDiagnosticValidation.Valid(fact, diagnostic, input.OriginalFacts, false));

    static bool LocalIdentity(ArtifactMemberRoleSelectionFact fact, EffectiveGenerationInputs input) =>
        input.OriginalFacts.Where(candidate => candidate.Id == fact.Id && candidate.Evidence.Adapter == fact.Evidence.Adapter).Select(Structural.Fact).Distinct(StringComparer.Ordinal).Count() == 1;

    static bool Supports(ArtifactMemberRoleSelectionFact fact, EffectiveGenerationInputs input) => input.OriginalFacts.OfType<MethodFactSupportFact>()
        .Where(support => GenerationFactInstance.CandidateOf(support) == GenerationFactInstance.Of(fact))
        .All(support => !ArtifactMemberRoleSelectionSupport.Invalid(support, input) && support.Definition.Alternatives?.All(alternative => alternative.Scope == fact.Definition.Artifact.Scope) == true);

    static bool KnownScope(ArtifactKey key, EffectiveGenerationInputs input)
    {
        if (key.Scope is null)
        {
            return true;
        }

        return key.Kind is not (ArtifactKind.Concept or ArtifactKind.Query) && BackendScopeFactResolver.ValidFacts([.. input.Facts.OfType<BackendScopeFact>()], [])
            .OfType<BackendScopeFact>().Select(scope => scope.Definition.Key).Contains(key.Scope);
    }

    static bool Supported<T>(T value, T unknown)
        where T : struct, Enum => !EqualityComparer<T>.Default.Equals(value, unknown) && Enum.IsDefined(value);
}
