// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactUsageAdmissionValidator
{
    public static void Validate(GenerationFact fact, string path, AdapterContributionAdmissionContext context)
    {
        switch (fact)
        {
            case ArtifactUsageDeclarationFact declaration:
                ValidateOwner(fact, declaration.Definition.Declaration, $"{path}.Definition.Declaration", context);
                AdapterContributionAdmissionValidator.ValidateArtifactKey(declaration.Definition.Artifact, $"{path}.Definition.Artifact", fact.Id, context);
                break;
            case EntryPointFact entry:
                ValidateOwner(fact, entry.Definition.Method, $"{path}.Definition.Method", context);
                AdapterContributionAdmissionValidator.ValidateArtifactKey(entry.Definition.Artifact, $"{path}.Definition.Artifact", fact.Id, context);
                break;
            case MethodArtifactUsageFact usage:
                ValidateOwner(fact, usage.Definition.Method, $"{path}.Definition.Method", context);
                AdapterContributionAdmissionValidator.ValidateSubject(usage.Definition.Declaration, $"{path}.Definition.Declaration", fact.Id, context);
                context.Enum(usage.Definition.Kind, RelationshipKind.Unknown, $"{path}.Definition.Kind", fact.Id, fact.Subject);
                break;
        }
    }

    static void ValidateOwner(GenerationFact fact, SubjectId owner, string path, AdapterContributionAdmissionContext context)
    {
        AdapterContributionAdmissionValidator.ValidateSubject(owner, path, fact.Id, context);
        if (owner != fact.Subject)
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, path, "The usage declaration or method must equal the fact subject", fact.Id, fact.Subject);
        }
    }
}
