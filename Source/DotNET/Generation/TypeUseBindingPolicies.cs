// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseBindingPolicies
{
    public static bool Supported(TypeUseBindingPolicyFact policy) =>
        policy.Definition.AllowedTargetKind == ArtifactKind.Concept &&
        policy.Definition.Fallback == TypeUseBindingFallback.PreserveObservedTypeWithoutTarget &&
        policy.Definition.Member.Artifact.Kind != ArtifactKind.Unknown && Enum.IsDefined(policy.Definition.Member.Artifact.Kind);

    public static void Validate(GenerationFact fact, IReadOnlyList<GenerationFact> facts, List<GenerationDiagnostic> diagnostics)
    {
        if (fact is not TypeUseBindingPolicyFact policy || Valid(policy, facts))
        {
            return;
        }

        diagnostics.Add(new()
        {
            Code = GenerationDiagnosticCodes.InvalidTypeUseBindingPolicy,
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = "A concept-only binding policy requires an exact same-producer observed type use and warning template",
            Subject = policy.Subject,
            Source = policy.Evidence.Source,
            Facts = [policy.Id]
        });
    }

    static bool Valid(TypeUseBindingPolicyFact policy, IReadOnlyList<GenerationFact> facts) =>
        Supported(policy) && policy.Subject == policy.Definition.Member.Artifact.Subject &&
        !string.IsNullOrWhiteSpace(policy.Definition.Member.Name) && ValidWarning(policy, facts) && MatchesObservation(policy, facts);

    static bool MatchesObservation(TypeUseBindingPolicyFact policy, IReadOnlyList<GenerationFact> facts)
    {
        var typeUses = facts.OfType<ArtifactMemberTypeUseFact>().Where(candidate => Matches(policy, candidate)).ToArray();
        var policies = facts.OfType<TypeUseBindingPolicyFact>().Where(candidate =>
            candidate.Evidence.Adapter == policy.Evidence.Adapter && candidate.Definition.Member == policy.Definition.Member);
        return policies.Count() == 1 && typeUses.Length > 0 && typeUses.All(candidate => candidate.Definition.Type.ObservedTypeSubject is not null);
    }

    static bool ValidWarning(TypeUseBindingPolicyFact policy, IReadOnlyList<GenerationFact> facts)
    {
        var diagnostic = policy.Definition.Diagnostic;
        var ownedIds = facts.Where(fact => fact.Evidence.Adapter == policy.Evidence.Adapter).Select(fact => fact.Id).ToHashSet();
        return Warning(diagnostic) && diagnostic.Subject == policy.Subject && diagnostic.Source == policy.Evidence.Source && diagnostic.Facts.All(ownedIds.Contains);
    }

    static bool Warning(GenerationDiagnostic diagnostic) => diagnostic.Severity == GenerationDiagnosticSeverity.Warning &&
        !string.IsNullOrWhiteSpace(diagnostic.Code) && !string.IsNullOrWhiteSpace(diagnostic.Message);

    static bool Matches(TypeUseBindingPolicyFact policy, ArtifactMemberTypeUseFact typeUse) =>
        policy.Definition.Member == typeUse.Definition.Member && policy.Subject == typeUse.Subject && policy.Evidence.Adapter == typeUse.Evidence.Adapter;
}
