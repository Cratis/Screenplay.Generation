// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class OccurrenceOmissionDiagnosticFreezer
{
    public static GenerationFact? Freeze(GenerationFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        if (fact is not OccurrenceOmissionDiagnosticFact candidate)
        {
            return null;
        }

        var definition = candidate.Definition;
        var path = $"{envelope.Path}.Definition";
        if (definition?.Family is null)
        {
            context.Missing($"{path}.Family");
        }

        return new OccurrenceOmissionDiagnosticFact
        {
            Id = envelope.Id, Subject = envelope.Subject, Evidence = envelope.Evidence,
            Definition = new()
            {
                Family = new() { Value = definition?.Family?.Value ?? string.Empty },
                Occurrence = GranularFactFreezer.FreezeArtifactKey(definition?.Occurrence, $"{path}.Occurrence", context),
                Policy = definition?.Policy ?? OccurrenceOmissionDiagnosticPolicy.Unknown,
                Preference = definition?.Preference ?? OmissionDiagnosticEvidencePreference.Unknown,
                Diagnostic = DiagnosticIntentFreezer.Payload(definition?.Diagnostic, $"{path}.Diagnostic", context)
            }
        };
    }
}
