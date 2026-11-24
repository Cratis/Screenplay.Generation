// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class ArtifactMemberRoleSelectionFreezer
{
    public static ArtifactMemberRoleSelectionFact Freeze(ArtifactMemberRoleSelectionFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        var definition = fact.Definition;
        var path = $"{envelope.Path}.Definition";
        if (definition is null)
        {
            context.Missing(path);
        }

        return new()
        {
            Id = envelope.Id, Subject = envelope.Subject, Evidence = envelope.Evidence,
            Definition = new()
            {
                Artifact = GranularFactFreezer.FreezeArtifactKey(definition?.Artifact, $"{path}.Artifact", context),
                Role = definition?.Role ?? ArtifactMemberRoleKind.Unknown,
                Member = definition?.Member is { } member ? new() { Artifact = GranularFactFreezer.FreezeArtifactKey(member.Artifact, $"{path}.Member.Artifact", context), Name = member.Name ?? string.Empty } : null,
                Priority = definition?.Priority ?? 0, ObservationOrder = definition?.ObservationOrder ?? -1,
                Diagnostics = Diagnostics(definition?.Diagnostics, $"{path}.Diagnostics", context)
            }
        };
    }

    static GenerationDiagnostic[] Diagnostics(IReadOnlyList<GenerationDiagnostic>? diagnostics, string path, AdapterContributionAdmissionContext context)
    {
        if (diagnostics is null)
        {
            context.Missing(path);
            return [];
        }

        return [.. diagnostics.Select((diagnostic, index) => DiagnosticIntentFreezer.Payload(diagnostic, $"{path}[{index}]", context))];
    }
}
