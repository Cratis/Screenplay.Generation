// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

static class OmissionDiagnosticGroupFreezer
{
    public static GenerationFact? Freeze(GenerationFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        if (fact is not OmittedFactDiagnosticGroupFact group)
        {
            return null;
        }

        var path = $"{envelope.Path}.Definition";
        if (group.Definition is null)
        {
            context.Missing(path);
        }

        return new OmittedFactDiagnosticGroupFact
        {
            Id = envelope.Id, Subject = envelope.Subject, Evidence = envelope.Evidence,
            Definition = new()
            {
                CoveredFacts = Covered(group.Definition?.CoveredFacts, $"{path}.CoveredFacts", context),
                Diagnostic = DiagnosticIntentFreezer.Payload(group.Definition?.Diagnostic, $"{path}.Diagnostic", context)
            }
        };
    }

    static ImmutableArray<FactId> Covered(IReadOnlyList<FactId>? covered, string path, AdapterContributionAdmissionContext context)
    {
        if (covered is null)
        {
            context.Missing(path);
            return [];
        }

        return [.. covered.Select((id, index) => id is null ? Missing($"{path}[{index}]", context) : new FactId { Value = id.Value }).OrderBy(id => id.Value, StringComparer.Ordinal)];
    }

    static FactId Missing(string path, AdapterContributionAdmissionContext context)
    {
        context.Missing(path);
        return new() { Value = string.Empty };
    }
}
