// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class TypeUseTargetOmissionAdmissionValidator
{
    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        var typeUses = facts.OfType<ArtifactMemberTypeUseFact>().ToLookup(fact => fact.Definition.Member);
        foreach (var omission in facts.OfType<TypeUseTargetOmittedFact>())
        {
            var candidates = typeUses[omission.Definition.Member].ToArray();
            if (candidates.Length == 0 || candidates.Any(candidate => !Matches(candidate, omission)))
            {
                context.Add(
                    AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch,
                    $"Contribution.Facts[{omission.Id}].Definition.Member",
                    "Target omission must identify an authored type use with the same subject and producer in this contribution",
                    omission.Id,
                    omission.Subject);
            }

            AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), omission.Definition.Diagnostic, context);
        }
    }

    static bool Matches(ArtifactMemberTypeUseFact candidate, TypeUseTargetOmittedFact omission) =>
        candidate.Definition.Type.ObservedTypeSubject is not null &&
        candidate.Subject == omission.Subject && candidate.Evidence.Adapter == omission.Evidence.Adapter;
}
