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

        if (fact.Definition?.TypeUse is null)
        {
            context.Missing($"{envelope.Path}.Definition.TypeUse");
        }

        return new()
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = new()
            {
                TypeUse = new() { Value = fact.Definition?.TypeUse?.Value ?? string.Empty },
                Policy = fact.Definition?.Policy ?? TypeUseBindingPolicyKind.Unknown
            }
        };
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, AdapterContributionAdmissionContext context)
    {
        foreach (var policy in facts.OfType<TypeUseBindingPolicyFact>())
        {
            var observations = facts.Where(fact => fact.Id == policy.Definition.TypeUse).ToArray();
            if (observations.Length != 1 || !Matches(policy, observations[0]))
            {
                Invalid(policy, "A binding policy requires one exact same-producer type observation", context);
            }

            if (facts.OfType<TypeUseBindingPolicyFact>().Count(candidate => candidate.Definition.TypeUse == policy.Definition.TypeUse) != 1)
            {
                Invalid(policy, "A type observation must have exactly one binding policy", context);
            }
        }
    }

    public static void ValidateShape(TypeUseBindingPolicyFact policy, string path, AdapterContributionAdmissionContext context)
    {
        context.Enum(policy.Definition.Policy, TypeUseBindingPolicyKind.Unknown, $"{path}.Definition.Policy", policy.Id, policy.Subject);
        AdapterContributionAdmissionValidator.ValidateRequiredText(policy.Definition.TypeUse.Value, $"{path}.Definition.TypeUse", policy.Id, policy.Subject, context);
    }

    static bool Matches(TypeUseBindingPolicyFact policy, GenerationFact observation) =>
        ExactObservation(observation) && observation.Subject == policy.Subject && SameEvidence(policy.Evidence, observation.Evidence);

    static bool SameEvidence(Evidence policy, Evidence observation) =>
        policy.Adapter == observation.Adapter && policy.Source == observation.Source && policy.Strength == EvidenceStrength.Exact && observation.Strength == EvidenceStrength.Exact;

    static bool ExactObservation(GenerationFact observation) => observation switch
    {
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Type.ObservedTypeSubject is not null,
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Type.ObservedTypeSubject is not null,
        _ => false
    };

    static void Invalid(TypeUseBindingPolicyFact policy, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"Contribution.Facts[{policy.Id}].Definition", message, policy.Id, policy.Subject);
}
