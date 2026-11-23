// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class SourcePlacementCandidateFreezer
{
    public static ArtifactSourcePlacementCandidateFact Freeze(ArtifactSourcePlacementCandidateFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        if (definition is null) context.Missing($"{envelope.Path}.Definition");
        if (definition?.Policy is null) context.Missing($"{envelope.Path}.Definition.Policy");
        return new ArtifactSourcePlacementCandidateFact
        {
            Id = envelope.Id, Subject = envelope.Subject, Evidence = envelope.Evidence,
            Definition = new ArtifactSourcePlacementCandidateDefinition
            {
                Artifact = AdapterContributionFreezer.FreezeArtifactKey(definition?.Artifact, $"{envelope.Path}.Definition.Artifact", context),
                Owner = AdapterContributionFreezer.FreezeArtifactKey(definition?.Owner, $"{envelope.Path}.Definition.Owner", context),
                SliceKind = definition?.SliceKind ?? GenerationSliceKind.Unknown,
                Reason = definition?.Reason ?? ArtifactSourcePlacementReason.Unknown,
                Policy = definition?.Policy is { } policy ? policy with { } : new ArtifactSourcePlacementSelectionPolicy()
            }
        };
    }
}
