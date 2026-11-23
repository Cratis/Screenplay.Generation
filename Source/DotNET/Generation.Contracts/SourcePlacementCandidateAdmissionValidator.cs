// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class SourcePlacementCandidateAdmissionValidator
{
    public static void Validate(ArtifactSourcePlacementCandidateFact fact, string path, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        AdapterContributionAdmissionValidator.ValidateArtifactKey(definition.Artifact, $"{path}.Definition.Artifact", fact.Id, context);
        AdapterContributionAdmissionValidator.ValidateArtifactKey(definition.Owner, $"{path}.Definition.Owner", fact.Id, context);
        context.Enum(definition.SliceKind, GenerationSliceKind.Unknown, $"{path}.Definition.SliceKind", fact.Id, fact.Subject);
        context.Enum(definition.Reason, ArtifactSourcePlacementReason.Unknown, $"{path}.Definition.Reason", fact.Id, fact.Subject);
        context.Enum(definition.Policy.Mode, ArtifactSourcePlacementSelectionMode.Unknown, $"{path}.Definition.Policy.Mode", fact.Id, fact.Subject);
        if (definition.Policy.Version != 1 || !HasValidOwnership(fact))
        {
            context.Add(AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch, $"{path}.Definition", "Source-owner candidates require version 1 and exact self ownership or an event owned by a producing command with exact endpoint scopes", fact.Id, fact.Subject);
        }
    }

    public static bool HasValidOwnership(ArtifactSourcePlacementCandidateFact fact) =>
        fact.Subject == fact.Definition.Artifact.Subject && fact.Definition.Reason switch
        {
            ArtifactSourcePlacementReason.Self => fact.Definition.Owner == fact.Definition.Artifact,
            ArtifactSourcePlacementReason.ProducingCommand => fact.Definition.Artifact.Kind == ArtifactKind.Event && fact.Definition.Owner.Kind == ArtifactKind.Command,
            _ => false
        };
}
