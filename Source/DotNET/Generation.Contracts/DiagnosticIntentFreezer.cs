// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

static class DiagnosticIntentFreezer
{
    public static GenerationFact? Freeze(GenerationFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context)
    {
        if (fact is not MethodDiagnosticProjectionFact projection)
        {
            return null;
        }

        var definition = projection.Definition;
        var path = $"{envelope.Path}.Definition";
        if (definition is null)
        {
            context.Missing(path);
        }

        return new MethodDiagnosticProjectionFact
        {
            Id = envelope.Id, Subject = envelope.Subject, Evidence = envelope.Evidence,
            Definition = new()
            {
                Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Method", context),
                Scope = BackendScopeFreezer.Key(definition?.Scope, $"{path}.Scope", context),
                EntryPointRole = definition?.EntryPointRole ?? ArtifactKind.Unknown,
                Mode = definition?.Mode ?? MethodDiagnosticProjectionMode.Unknown,
                Diagnostic = Payload(definition?.Diagnostic, $"{path}.Diagnostic", context)
            }
        };
    }

    static GenerationDiagnostic Payload(GenerationDiagnostic? diagnostic, string path, AdapterContributionAdmissionContext context)
    {
        if (diagnostic is null)
        {
            context.Missing(path);
            return new() { Code = string.Empty, Severity = GenerationDiagnosticSeverity.Unknown, Message = string.Empty };
        }

        return AdapterContributionFreezer.FreezeMethodDiagnostic(diagnostic, path, context);
    }
}
