// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionAdmission
{
    public static void ValidateShape(ArtifactMemberRoleSelectionFact fact, string path, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        AdapterContributionAdmissionValidator.ValidateArtifactKey(definition.Artifact, $"{path}.Definition.Artifact", fact.Id, context);
        BackendScopeFactAdmissionValidator.ValidateKey(definition.Artifact.Scope, $"{path}.Definition.Artifact.Scope", (fact.Id, fact.Subject), context);
        context.Enum(definition.Role, ArtifactMemberRoleKind.Unknown, $"{path}.Definition.Role", fact.Id, fact.Subject);
        if (definition.Artifact.Subject != fact.Subject || definition.ObservationOrder < 0)
        {
            Invalid(fact, "Role selection requires a matching artifact subject and nonnegative observation order", context);
        }

        if (definition.Member is { } member)
        {
            GranularFactAdmissionValidator.Validate(new ArtifactMemberRoleFact { Id = fact.Id, Subject = fact.Subject, Evidence = fact.Evidence, Definition = new() { Member = member, Role = definition.Role } }, path, context);
            if (member.Artifact != definition.Artifact)
            {
                Invalid(fact, "Selected member must belong to the exact artifact occurrence", context);
            }
        }
    }

    public static void Validate(IReadOnlyList<GenerationFact> facts, string adapter, ISourceAuthorityValidator? authority, AdapterContributionAdmissionContext context)
    {
        var ids = facts.Select(fact => fact.Id.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var candidate in facts.OfType<ArtifactMemberRoleSelectionFact>())
        {
            foreach (var diagnostic in candidate.Definition.Diagnostics)
            {
                AdapterContributionAdmissionValidator.ValidateMethodDiagnostic((adapter, ids, authority), diagnostic, context);
                if (diagnostic.Subject != candidate.Subject)
                {
                    Invalid(candidate, "Selection diagnostic subject must match its candidate", context);
                }
            }

            var alternatives = facts.OfType<MethodFactSupportFact>().Where(support => support.Definition.Candidate == candidate.Id).SelectMany(support => support.Definition.Alternatives);
            if (alternatives.Any(alternative => alternative.Scope != candidate.Definition.Artifact.Scope))
            {
                Invalid(candidate, "Role selection method support must match its exact artifact scope", context);
            }
        }
    }

    static void Invalid(GenerationFact fact, string message, AdapterContributionAdmissionContext context) =>
        context.Add(AdapterContributionAdmissionDiagnosticCode.InvalidKindOperand, $"Contribution.Facts[{fact.Id}].Definition", message, fact.Id, fact.Subject);
}
