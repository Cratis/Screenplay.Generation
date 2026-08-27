// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;

namespace Cratis.Screenplay.Generation.DotNet;

static class DotNetAdapterProbeAdmission
{
    public static AdapterProbeResult Admit(
        AdapterDescriptor descriptor,
        AdapterProbeResult? probe,
        ISourceAuthorityValidator sourceAuthorityValidator)
    {
        if (probe is not AdapterProbeApplicable and not AdapterProbeNotApplicable and not AdapterProbeBlocked)
        {
            return Malformed(descriptor.Identity.Id);
        }

        if (!TryFreezeEvidence(probe.Evidence, sourceAuthorityValidator, out var evidence))
        {
            return Malformed(descriptor.Identity.Id);
        }

        if (probe is AdapterProbeBlocked blocked)
        {
            return TryFreezeDiagnostics(descriptor, blocked.Diagnostics, sourceAuthorityValidator, out var diagnostics) &&
                   !diagnostics.IsEmpty
                ? new AdapterProbeBlocked { Evidence = evidence, Diagnostics = diagnostics }
                : Malformed(descriptor.Identity.Id);
        }

        if (probe is AdapterProbeNotApplicable)
        {
            return new AdapterProbeNotApplicable { Evidence = evidence };
        }

        var missing = descriptor.RequiredApiCapabilities
            .Where(required => !evidence.Any(item => item.ApiCapability == required))
            .ToArray();
        if (missing.Length == 0)
        {
            return new AdapterProbeApplicable { Evidence = evidence };
        }

        return new AdapterProbeBlocked
        {
            Evidence = evidence,
            Diagnostics =
            [
                .. missing.Select(capability => DotNetAdapterDiagnostics.Error(
                    DotNetAdapterGenerationDiagnosticCodes.MissingApiCapabilityEvidence,
                    descriptor.Identity.Id,
                    $"applicable probe did not prove required API capability '{capability.Id}'"))
            ]
        };
    }

    public static AdapterProbeBlocked Malformed(string adapterId) => new()
    {
        Diagnostics =
        [
            DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.ProbeRejected,
                adapterId,
                "returned a malformed structured probe result")
        ]
    };

    static bool TryFreezeEvidence(
        ImmutableArray<AdapterProbeEvidence> source,
        ISourceAuthorityValidator validator,
        out ImmutableArray<AdapterProbeEvidence> evidence)
    {
        evidence = [];
        if (source.IsDefault)
        {
            return true;
        }

        var frozen = ImmutableArray.CreateBuilder<AdapterProbeEvidence>();
        foreach (var item in source)
        {
            if (item is null ||
                !IsNormalized(item.Description, true) ||
                (item.ApiCapability is not null && !IsNormalized(item.ApiCapability.Id, false)) ||
                (item.Subject is not null && !IsSubject(item.Subject.Value)) ||
                (item.Source is not null && !validator.IsAuthoritative(item.Source)))
            {
                return false;
            }

            frozen.Add(new AdapterProbeEvidence
            {
                Description = item.Description,
                ApiCapability = item.ApiCapability is null
                    ? null
                    : new AdapterApiCapability { Id = item.ApiCapability.Id },
                Source = item.Source is null ? null : Freeze(item.Source),
                Subject = item.Subject is null ? null : new SubjectId { Value = item.Subject.Value }
            });
        }

        evidence =
        [
            .. frozen
                .OrderBy(item => item.ApiCapability?.Id, StringComparer.Ordinal)
                .ThenBy(item => item.Source?.FileIdentity?.Project, StringComparer.Ordinal)
                .ThenBy(item => item.Source?.FileIdentity?.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Source?.Path, StringComparer.Ordinal)
                .ThenBy(item => item.Source?.StartLine)
                .ThenBy(item => item.Source?.StartColumn)
                .ThenBy(item => item.Source?.EndLine)
                .ThenBy(item => item.Source?.EndColumn)
                .ThenBy(item => item.Subject?.Value, StringComparer.Ordinal)
                .ThenBy(item => item.Description, StringComparer.Ordinal)
        ];
        return true;
    }

    static bool TryFreezeDiagnostics(
        AdapterDescriptor descriptor,
        ImmutableArray<GenerationDiagnostic> diagnostics,
        ISourceAuthorityValidator validator,
        out ImmutableArray<GenerationDiagnostic> frozen)
    {
        var admission = AdapterContributionAdmission.Admit(
            descriptor,
            new AdapterContribution
            {
                Adapter = descriptor.Identity,
                Diagnostics = diagnostics.IsDefault ? [] : diagnostics
            },
            validator);
        frozen = admission.Snapshot?.Diagnostics ?? [];
        return admission.IsAdmitted;
    }

    static bool IsNormalized(string? value, bool allowWhitespace)
    {
        if (string.IsNullOrEmpty(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            value.Any(character => char.IsControl(character) || (!allowWhitespace && char.IsWhiteSpace(character))))
        {
            return false;
        }

        try
        {
            return string.Equals(value, value.Normalize(NormalizationForm.FormC), StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static bool IsSubject(string? value)
    {
        if (!IsNormalized(value, false) ||
            value!.Contains('\\') ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            string.IsNullOrWhiteSpace(uri.Scheme))
        {
            return false;
        }

        var schemeSeparator = value.IndexOf(':', StringComparison.Ordinal);
        var pathStart = schemeSeparator + 1;
        if (value.AsSpan(pathStart).StartsWith("//", StringComparison.Ordinal))
        {
            pathStart = value.IndexOf('/', pathStart + 2);
            if (pathStart < 0)
            {
                return true;
            }
        }

        var pathEnd = value.IndexOfAny(['?', '#'], pathStart);
        var path = pathEnd < 0 ? value[pathStart..] : value[pathStart..pathEnd];
        return !path.Split('/').Any(IsDotSegment);
    }

    static bool IsDotSegment(string segment)
    {
        var unescaped = Uri.UnescapeDataString(segment);
        return string.Equals(unescaped, ".", StringComparison.Ordinal) ||
               string.Equals(unescaped, "..", StringComparison.Ordinal);
    }

    static SourceRange Freeze(SourceRange source) => new()
    {
        Path = source.Path,
        FileIdentity = source.FileIdentity is null
            ? null
            : new SourceFileIdentity
            {
                Project = source.FileIdentity.Project,
                Path = source.FileIdentity.Path
            },
        StartLine = source.StartLine,
        StartColumn = source.StartColumn,
        EndLine = source.EndLine,
        EndColumn = source.EndColumn
    };
}
