// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseBindingPolicyAdmission
{
    public static TypeUseBindingPolicyFact Freeze(TypeUseBindingPolicyFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        if (fact.Definition is null)
        {
            context.Missing($"{envelope.Path}.Definition");
        }

        if (fact.Definition?.Diagnostic is null)
        {
            context.Missing($"{envelope.Path}.Definition.Diagnostic");
        }

        return new()
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = new()
            {
                Member = GranularFactFreezer.FreezeMemberKey(fact.Definition?.Member, $"{envelope.Path}.Definition.Member", context),
                AllowedTargetKind = fact.Definition?.AllowedTargetKind ?? ArtifactKind.Unknown,
                Fallback = fact.Definition?.Fallback ?? TypeUseBindingFallback.Unknown,
                Diagnostic = AdapterContributionFreezer.FreezeMethodDiagnostic(
                    fact.Definition?.Diagnostic ?? new GenerationDiagnostic { Code = string.Empty, Message = string.Empty, Severity = GenerationDiagnosticSeverity.Unknown },
                    $"{envelope.Path}.Definition.Diagnostic",
                    context)
            }
        };
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var policy in facts.OfType<TypeUseBindingPolicyFact>())
        {
            if (!Matches(policy, facts))
            {
                Invalid(policy, "A binding policy requires one exact same-producer observed type use", context);
            }

            ValidatePolicy(policy, context);
            AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), policy.Definition.Diagnostic, context);
        }
    }

    public static void ValidateShape(TypeUseBindingPolicyFact policy, string path, AdapterContributionAdmissionContext context)
    {
        var member = policy.Definition.Member;
        AdapterContributionAdmissionValidator.ValidateArtifactKey(member.Artifact, $"{path}.Definition.Member.Artifact", policy.Id, context);
        AdapterContributionAdmissionValidator.ValidateRequiredText(member.Name, $"{path}.Definition.Member.Name", policy.Id, policy.Subject, context);
    }

    static void ValidatePolicy(TypeUseBindingPolicyFact policy, AdapterContributionAdmissionContext context)
    {
        var path = $"Contribution.Facts[{policy.Id}].Definition";
        context.Enum(policy.Definition.AllowedTargetKind, ArtifactKind.Unknown, $"{path}.AllowedTargetKind", policy.Id, policy.Subject);
        context.Enum(policy.Definition.Fallback, TypeUseBindingFallback.Unknown, $"{path}.Fallback", policy.Id, policy.Subject);
        if (policy.Definition.AllowedTargetKind != ArtifactKind.Concept || policy.Definition.Fallback != TypeUseBindingFallback.PreserveObservedTypeWithoutTarget)
        {
            Invalid(policy, "Only concept binding with observed-type preservation is supported", context);
        }

        if (!ValidWarning(policy))
        {
            Invalid(policy, "A binding policy requires a producer-owned warning template", context);
        }
    }

    static bool ValidWarning(TypeUseBindingPolicyFact policy)
    {
        var diagnostic = policy.Definition.Diagnostic;
        return diagnostic.Severity == GenerationDiagnosticSeverity.Warning && diagnostic.Subject == policy.Subject && diagnostic.Source == policy.Evidence.Source;
    }

    static bool Matches(TypeUseBindingPolicyFact policy, IReadOnlyList<GenerationFact> facts)
    {
        var typeUses = facts.OfType<ArtifactMemberTypeUseFact>().Where(fact => fact.Definition.Member == policy.Definition.Member).ToArray();
        return typeUses.Length > 0 && typeUses.All(fact => SameObservation(policy, fact)) &&
            facts.OfType<TypeUseBindingPolicyFact>().Count(fact => fact.Definition.Member == policy.Definition.Member) == 1;
    }

    static bool SameObservation(TypeUseBindingPolicyFact policy, ArtifactMemberTypeUseFact typeUse) =>
        policy.Subject == policy.Definition.Member.Artifact.Subject && typeUse.Subject == policy.Subject &&
        typeUse.Evidence.Adapter == policy.Evidence.Adapter && typeUse.Definition.Type.ObservedTypeSubject is not null;

    static void Invalid(TypeUseBindingPolicyFact policy, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"Contribution.Facts[{policy.Id}].Definition", message, policy.Id, policy.Subject);
}
