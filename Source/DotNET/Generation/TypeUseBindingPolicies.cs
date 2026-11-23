// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseBindingPolicies
{
    public static bool Supported(TypeUseBindingPolicyFact policy) => policy.Definition?.Policy == TypeUseBindingPolicyKind.ConceptOnlyWithUnboundFallback;

    public static bool OptedIn(GenerationFact observation, IReadOnlyList<GenerationFact> facts)
    {
        var policies = facts.OfType<TypeUseBindingPolicyFact>().Where(policy => policy.Definition?.TypeUse == observation.Id && policy.Evidence.Adapter == observation.Evidence.Adapter).ToArray();
        return policies.Length == 1 && Matches(policies[0], observation) && Valid(policies[0], facts);
    }

    public static IReadOnlySet<ArtifactMemberKey> Members(IReadOnlyList<GenerationFact> facts) => facts.OfType<ArtifactMemberTypeUseFact>()
        .GroupBy(fact => fact.Definition.Member)
        .Where(group => group.All(observation => OptedIn(observation, facts)))
        .Select(group => group.Key).ToHashSet();

    public static IEnumerable<GenerationFact> ForMember(ArtifactMemberKey member, IReadOnlyList<GenerationFact> facts)
    {
        var observations = facts.OfType<ArtifactMemberTypeUseFact>().Where(fact => fact.Definition.Member == member).Select(fact => fact.Id).ToHashSet();
        return facts.OfType<TypeUseBindingPolicyFact>().Where(policy => policy.Definition is not null && observations.Contains(policy.Definition.TypeUse));
    }

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
            Message = "A binding policy requires one exact same-producer type observation and a supported policy",
            Subject = policy.Subject,
            Source = policy.Evidence.Source,
            Facts = [policy.Id]
        });
    }

    static bool Valid(TypeUseBindingPolicyFact policy, IReadOnlyList<GenerationFact> facts)
    {
        if (!Supported(policy))
        {
            return false;
        }

        var observations = facts.Where(fact => fact.Id == policy.Definition.TypeUse && fact.Evidence.Adapter == policy.Evidence.Adapter).ToArray();
        var policies = facts.OfType<TypeUseBindingPolicyFact>().Where(candidate => candidate.Evidence.Adapter == policy.Evidence.Adapter && candidate.Definition?.TypeUse == policy.Definition.TypeUse);
        return policies.Count() == 1 && observations.Length == 1 && Matches(policy, observations[0]);
    }

    static bool Matches(TypeUseBindingPolicyFact policy, GenerationFact observation) =>
        Supported(policy) && policy.Subject == observation.Subject && SameEvidence(policy.Evidence, observation.Evidence) && ExactObservation(observation);

    static bool SameEvidence(Evidence policy, Evidence observation) =>
        policy.Adapter == observation.Adapter && policy.Source == observation.Source && policy.Strength == EvidenceStrength.Exact && observation.Strength == EvidenceStrength.Exact;

    static bool ExactObservation(GenerationFact observation) => observation switch
    {
        ArtifactMemberTypeUseFact typeUse => typeUse.Definition.Type.ObservedTypeSubject is not null,
        DeclarationInputMemberTypeUseFact typeUse => typeUse.Definition.Type.ObservedTypeSubject is not null,
        _ => false
    };
}
