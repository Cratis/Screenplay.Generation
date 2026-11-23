// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Generation;

static class MethodEligibilityFreezer
{
    public static GenerationFact? Freeze(GenerationFact fact, FactEnvelope envelope, AdapterContributionAdmissionContext context) => fact switch
    {
        MethodExecutionClassificationFact classification => new MethodExecutionClassificationFact
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = Classification(classification.Definition, envelope.Path, context)
        },
        MethodFactSupportFact support => new MethodFactSupportFact
        {
            Id = envelope.Id,
            Subject = envelope.Subject,
            Evidence = envelope.Evidence,
            Definition = Support(support.Definition, envelope.Path, context)
        },
        _ => null
    };

    static MethodExecutionClassificationDefinition Classification(MethodExecutionClassificationDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        if (definition is null)
        {
            context.Missing($"{path}.Definition");
        }

        return new MethodExecutionClassificationDefinition
        {
            Method = GranularFactFreezer.FreezeSubject(definition?.Method, $"{path}.Definition.Method", context),
            Kind = definition?.Kind ?? MethodExecutionKind.Unknown,
            Scope = BackendScopeFreezer.Key(definition?.Scope, $"{path}.Definition.Scope", context),
            OmissionReason = definition?.OmissionReason is { } reason ? AdapterContributionFreezer.FreezeMethodDiagnostic(reason, $"{path}.Definition.OmissionReason", context) : null
        };
    }

    static MethodFactSupportDefinition Support(MethodFactSupportDefinition? definition, string path, AdapterContributionAdmissionContext context)
    {
        if (definition?.Candidate is null)
        {
            context.Missing($"{path}.Definition.Candidate");
        }

        if (definition?.Alternatives is null)
        {
            context.NullCollection($"{path}.Definition.Alternatives");
        }

        var alternatives = ImmutableArray.CreateBuilder<MethodFactSupportAlternative>();
        foreach (var alternative in definition?.Alternatives ?? [])
        {
            if (alternative is null)
            {
                context.Missing($"{path}.Definition.Alternatives[]");
                continue;
            }

            alternatives.Add(Alternative(alternative, path, context));
        }

        return new MethodFactSupportDefinition
        {
            Candidate = new FactId { Value = definition?.Candidate?.Value ?? string.Empty },
            Alternatives = [.. alternatives.GroupBy(alternative => JsonSerializer.Serialize(alternative), StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.First())]
        };
    }

    static MethodFactSupportAlternative Alternative(MethodFactSupportAlternative alternative, string path, AdapterContributionAdmissionContext context)
    {
        if (alternative.EnclosingExecutables is null)
        {
            context.NullCollection($"{path}.EnclosingExecutables");
        }

        return new MethodFactSupportAlternative
        {
            Method = GranularFactFreezer.FreezeSubject(alternative.Method, $"{path}.Method", context),
            Scope = BackendScopeFreezer.Key(alternative.Scope, $"{path}.Scope", context),
            EnclosingExecutables = [.. (alternative.EnclosingExecutables ?? []).Select(subject => GranularFactFreezer.FreezeSubject(subject, $"{path}.EnclosingExecutables[]", context)).Distinct().OrderBy(subject => subject.Value, StringComparer.Ordinal)],
            Diagnostics = AdapterContributionFreezer.FreezeMethodDiagnostics(alternative.Diagnostics, context)
        };
    }
}
