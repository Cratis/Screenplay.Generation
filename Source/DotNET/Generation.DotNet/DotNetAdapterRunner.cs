// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Executes a deterministic roster of modern and legacy .NET adapters through atomic contribution admission.
/// </summary>
public static class DotNetAdapterRunner
{
    /// <summary>
    /// Executes an adapter roster using the loaded Generation.Contracts assembly version and returns a deeply frozen canonical snapshot.
    /// </summary>
    /// <param name="roster">The modern and legacy adapter registrations.</param>
    /// <param name="context">The canonical .NET analysis context.</param>
    /// <param name="options">The adapter analysis options.</param>
    /// <returns>The immutable adapter run snapshot.</returns>
    public static AdapterRunSnapshot Run(
        IEnumerable<DotNetAdapterRegistration> roster,
        DotNetAnalysisContext context,
        DotNetAdapterOptions options) =>
        Run(roster, context, options, CurrentGenerationContractsVersion());

    /// <summary>
    /// Executes an adapter roster using an explicit Generation.Contracts host version and returns a deeply frozen canonical snapshot.
    /// </summary>
    /// <param name="roster">The modern and legacy adapter registrations.</param>
    /// <param name="context">The canonical .NET analysis context.</param>
    /// <param name="options">The adapter analysis options.</param>
    /// <param name="generationContractsVersion">The Generation.Contracts package or assembly version enforced by the host.</param>
    /// <returns>The immutable adapter run snapshot.</returns>
    public static AdapterRunSnapshot Run(
        IEnumerable<DotNetAdapterRegistration> roster,
        DotNetAnalysisContext context,
        DotNetAdapterOptions options,
        Version generationContractsVersion)
    {
        var registrations = roster.ToArray();
        var prepared = registrations
            .Select(Prepare)
            .Order(PreparedRegistrationComparer.Instance)
            .ToArray();
        var duplicateIds = prepared
            .Where(item => item.Failure is null)
            .GroupBy(item => item.Descriptor.Identity.Id, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var projectDiagnostics = ProjectDiagnostics(context);
        var hostCapabilities = DotNetHostCapabilities.Determine(context);
        var records = prepared
            .Select(item => Run(
                item,
                duplicateIds.Contains(item.Descriptor.Identity.Id),
                projectDiagnostics,
                hostCapabilities,
                generationContractsVersion,
                context,
                options))
            .ToImmutableArray();
        var facts = records
            .SelectMany(record => record.Execution is AdapterExecutionCompleted completed
                ? completed.Contribution.Facts
                : [])
            .OrderBy(fact => fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(fact => fact.Subject.Value, StringComparer.Ordinal)
            .ThenBy(fact => fact.GetType().FullName, StringComparer.Ordinal)
            .Select(fact => new GenerationFactRecord { Fact = fact })
            .ToImmutableArray();
        var diagnostics = DotNetAdapterDiagnostics.Canonical(projectDiagnostics.Concat(DiagnosticsFrom(records)));
        return new AdapterRunSnapshot
        {
            Adapters = records,
            Facts = facts,
            Diagnostics = [.. diagnostics]
        };
    }

    static PreparedRegistration Prepare(DotNetAdapterRegistration registration)
    {
        try
        {
            var admission = AdapterDescriptorAdmission.Admit(registration.Describe());
            return new PreparedRegistration(registration, admission.Descriptor, admission.Diagnostics, null);
        }
        catch (Exception exception)
        {
            var descriptor = AdapterDescriptorAdmission.Admit(FallbackDescriptor()).Descriptor;
            return new PreparedRegistration(
                registration,
                descriptor,
                [],
                DotNetAdapterDiagnostics.OperationFailed(descriptor.Identity.Id, "Descriptor", exception));
        }
    }

    static AdapterRunRecord Run(
        PreparedRegistration item,
        bool isDuplicate,
        ImmutableArray<GenerationDiagnostic> projectDiagnostics,
        ImmutableArray<AdapterHostCapability> hostCapabilities,
        Version generationContractsVersion,
        DotNetAnalysisContext context,
        DotNetAdapterOptions options)
    {
        if (item.Failure is not null)
        {
            return RejectedRosterRecord(item.Descriptor, [item.Failure]);
        }

        if (isDuplicate)
        {
            var diagnostic = DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.DuplicateAdapterId,
                item.Descriptor.Identity.Id,
                "was rejected because its identity occurs more than once in the roster");
            return RejectedRosterRecord(item.Descriptor, [diagnostic]);
        }

        if (!item.DescriptorDiagnostics.IsEmpty)
        {
            return RejectedRosterRecord(
                item.Descriptor,
                [.. item.DescriptorDiagnostics.Select(diagnostic => DescriptorDiagnostic(item.Descriptor, diagnostic))]);
        }

        if (!projectDiagnostics.IsEmpty && RequiresProjectRoster(item.Descriptor))
        {
            return BlockedBeforeProbe(item.Descriptor, projectDiagnostics);
        }

        var compatibilityDiagnostics = GenerationCompatibilityDiagnostics(item.Descriptor, generationContractsVersion);
        if (!compatibilityDiagnostics.IsEmpty)
        {
            return BlockedBeforeProbe(item.Descriptor, compatibilityDiagnostics);
        }

        var hostDiagnostics = HostDiagnostics(item.Descriptor, hostCapabilities);
        if (!hostDiagnostics.IsEmpty)
        {
            return BlockedBeforeProbe(item.Descriptor, hostDiagnostics);
        }

        return ProbeAndRun(item, context, options);
    }

    static AdapterRunRecord ProbeAndRun(
        PreparedRegistration item,
        DotNetAnalysisContext context,
        DotNetAdapterOptions options)
    {
        var requiresStableIdentity = item.Descriptor.RequiredHostCapabilities.Contains(AdapterHostCapability.StableSourceLocations);
        var sourceValidator = new DotNetSourceAuthorityValidator(context, requiresStableIdentity);
        AdapterProbeResult rawProbe;
        try
        {
            rawProbe = item.Registration.Probe(context);
        }
        catch (Exception exception)
        {
            var operation = item.Registration.IsLegacy ? "CanAnalyze" : "Probe";
            var diagnostic = DotNetAdapterDiagnostics.OperationFailed(item.Descriptor.Identity.Id, operation, exception);
            return new AdapterRunRecord
            {
                Considered = true,
                Probed = true,
                Descriptor = item.Descriptor,
                Probe = new AdapterProbeBlocked { Diagnostics = [diagnostic] },
                Execution = new AdapterExecutionFailed { Diagnostics = [diagnostic] },
                Disposition = AdapterRunDisposition.ExecutionFailed
            };
        }

        var probe = DotNetAdapterProbeAdmission.Admit(item.Descriptor, rawProbe, sourceValidator);
        return probe switch
        {
            AdapterProbeNotApplicable => NotApplicable(item.Descriptor, probe),
            AdapterProbeBlocked blocked => BlockedAfterProbe(item.Descriptor, blocked),
            AdapterProbeApplicable => Analyze(item, probe, sourceValidator, context, options),
            _ => BlockedAfterProbe(
                item.Descriptor,
                DotNetAdapterProbeAdmission.Malformed(item.Descriptor.Identity.Id))
        };
    }

    static AdapterRunRecord Analyze(
        PreparedRegistration item,
        AdapterProbeResult probe,
        ISourceAuthorityValidator sourceValidator,
        DotNetAnalysisContext context,
        DotNetAdapterOptions options)
    {
        AdapterContribution contribution;
        try
        {
            contribution = item.Registration.Analyze(context, options);
        }
        catch (Exception exception)
        {
            var diagnostic = DotNetAdapterDiagnostics.OperationFailed(item.Descriptor.Identity.Id, "Analyze", exception);
            return new AdapterRunRecord
            {
                Considered = true,
                Probed = true,
                Executed = true,
                Descriptor = item.Descriptor,
                Probe = probe,
                Execution = new AdapterExecutionFailed { Diagnostics = [diagnostic] },
                Disposition = AdapterRunDisposition.ExecutionFailed
            };
        }

        AdapterContributionAdmissionResult admission;
        try
        {
            admission = AdapterContributionAdmission.Admit(item.Descriptor, contribution, sourceValidator);
        }
        catch (Exception exception)
        {
            var diagnostic = DotNetAdapterDiagnostics.OperationFailed(item.Descriptor.Identity.Id, "ContributionAdmission", exception);
            return new AdapterRunRecord
            {
                Considered = true,
                Probed = true,
                Executed = true,
                Descriptor = item.Descriptor,
                Probe = probe,
                Execution = new AdapterExecutionFailed { Diagnostics = [diagnostic] },
                Disposition = AdapterRunDisposition.ExecutionFailed
            };
        }

        if (!admission.IsAdmitted)
        {
            var diagnostic = DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.ContributionRejected,
                item.Descriptor.Identity.Id,
                "contribution was rejected atomically");
            return new AdapterRunRecord
            {
                Considered = true,
                Probed = true,
                Executed = true,
                Descriptor = item.Descriptor,
                Probe = probe,
                Execution = new AdapterExecutionRejected
                {
                    Diagnostics = [diagnostic],
                    AdmissionDiagnostics = admission.Diagnostics
                },
                Disposition = AdapterRunDisposition.ContributionRejected
            };
        }

        var snapshot = admission.Snapshot!;
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = item.Descriptor,
            Probe = probe,
            Execution = new AdapterExecutionCompleted
            {
                Diagnostics = snapshot.Diagnostics,
                Contribution = snapshot
            },
            Disposition = AdapterRunDisposition.Admitted
        };
    }

    static AdapterRunRecord RejectedRosterRecord(
        AdapterDescriptor descriptor,
        ImmutableArray<GenerationDiagnostic> diagnostics) => new()
        {
            Considered = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeNotRun(),
            Execution = new AdapterExecutionNotRun { Diagnostics = diagnostics },
            Disposition = AdapterRunDisposition.RosterRejected
        };

    static AdapterRunRecord BlockedBeforeProbe(
        AdapterDescriptor descriptor,
        ImmutableArray<GenerationDiagnostic> diagnostics) => new()
        {
            Considered = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeBlocked { Diagnostics = diagnostics },
            Execution = new AdapterExecutionNotRun { Diagnostics = diagnostics },
            Disposition = AdapterRunDisposition.Blocked
        };

    static AdapterRunRecord BlockedAfterProbe(AdapterDescriptor descriptor, AdapterProbeBlocked probe) => new()
    {
        Considered = true,
        Probed = true,
        Descriptor = descriptor,
        Probe = probe,
        Execution = new AdapterExecutionNotRun { Diagnostics = probe.Diagnostics },
        Disposition = AdapterRunDisposition.Blocked
    };

    static AdapterRunRecord NotApplicable(AdapterDescriptor descriptor, AdapterProbeResult probe) => new()
    {
        Considered = true,
        Probed = true,
        Descriptor = descriptor,
        Probe = probe,
        Disposition = AdapterRunDisposition.NotApplicable
    };

    static bool RequiresProjectRoster(AdapterDescriptor descriptor) =>
        descriptor.SourceLanguage != AdapterSourceLanguage.SourceIndependent ||
        !descriptor.RequiredHostCapabilities.IsEmpty;

    static ImmutableArray<GenerationDiagnostic> ProjectDiagnostics(DotNetAnalysisContext context)
    {
        var diagnostics = ImmutableArray.CreateBuilder<GenerationDiagnostic>();
        foreach (var duplicate in context.Projects
                     .Where(project => project.SourceContext is not null)
                     .GroupBy(project => project.SourceContext!.ProjectIdentity, StringComparer.Ordinal)
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            diagnostics.Add(DotNetAdapterDiagnostics.HostError(
                DotNetAdapterGenerationDiagnosticCodes.InvalidProjectRoster,
                $"rejected the project roster because stable project identity '{duplicate.Key}' occurs more than once"));
        }

        foreach (var duplicate in context.Projects
                     .Where(project => project.SourceContext is null)
                     .GroupBy(project => (project.Name, project.Compilation.AssemblyName))
                     .Where(group => group.Count() > 1)
                     .OrderBy(group => group.Key.Name, StringComparer.Ordinal)
                     .ThenBy(group => group.Key.AssemblyName, StringComparer.Ordinal))
        {
            var projectPaths = duplicate.Select(project => project.ProjectPath).ToArray();
            if (projectPaths.All(IsPortableRelativeProjectPath) &&
                projectPaths.Distinct(StringComparer.Ordinal).Count() == projectPaths.Length)
            {
                continue;
            }

            diagnostics.Add(DotNetAdapterDiagnostics.HostError(
                DotNetAdapterGenerationDiagnosticCodes.InvalidProjectRoster,
                $"rejected the project roster because legacy project name '{duplicate.Key.Name}' and assembly '{duplicate.Key.AssemblyName}' are not disambiguated by unique portable relative project paths"));
        }

        return DotNetAdapterDiagnostics.Canonical(diagnostics);
    }

    static ImmutableArray<GenerationDiagnostic> GenerationCompatibilityDiagnostics(
        AdapterDescriptor descriptor,
        Version generationContractsVersion)
    {
        var range = descriptor.CompatibleGenerationVersions;
        if (generationContractsVersion.CompareTo(range.MinimumInclusive) >= 0 &&
            (range.MaximumExclusive is null || generationContractsVersion.CompareTo(range.MaximumExclusive) < 0))
        {
            return [];
        }

        var maximum = range.MaximumExclusive is null ? "unbounded" : $"'{range.MaximumExclusive}' exclusive";
        return
        [
            DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.IncompatibleGenerationVersion,
                descriptor.Identity.Id,
                $"supports Generation.Contracts versions from '{range.MinimumInclusive}' inclusive through {maximum}, but the runner host version is '{generationContractsVersion}'")
        ];
    }

    static bool IsPortableRelativeProjectPath(string? path)
    {
        if (string.IsNullOrEmpty(path) ||
            !string.Equals(path, path.Trim(), StringComparison.Ordinal) ||
            path.Any(char.IsControl) ||
            path[0] == '/' ||
            path.Contains('\\') ||
            (path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':'))
        {
            return false;
        }

        try
        {
            if (!string.Equals(path, path.Normalize(), StringComparison.Ordinal))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        var segments = path.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (string.IsNullOrEmpty(segments[index]) ||
                !TryDecodeProjectPathSegment(segments[index], out var decoded) ||
                string.Equals(decoded, ".", StringComparison.Ordinal) ||
                string.Equals(decoded, "..", StringComparison.Ordinal) ||
                decoded.Contains('/') ||
                decoded.Contains('\\') ||
                (index == 0 && decoded.Length >= 2 && char.IsAsciiLetter(decoded[0]) && decoded[1] == ':'))
            {
                return false;
            }
        }

        return true;
    }

    static bool TryDecodeProjectPathSegment(string segment, out string decoded)
    {
        decoded = segment;
        try
        {
            while (true)
            {
                var unescaped = Uri.UnescapeDataString(decoded);
                if (string.Equals(unescaped, decoded, StringComparison.Ordinal))
                {
                    return true;
                }

                decoded = unescaped;
            }
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    static ImmutableArray<GenerationDiagnostic> HostDiagnostics(
        AdapterDescriptor descriptor,
        ImmutableArray<AdapterHostCapability> available)
    {
        var diagnostics = ImmutableArray.CreateBuilder<GenerationDiagnostic>();
        if (descriptor.SourceLanguage is not AdapterSourceLanguage.CSharp and not AdapterSourceLanguage.SourceIndependent)
        {
            diagnostics.Add(DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.UnsupportedSourceLanguage,
                descriptor.Identity.Id,
                $"requires unsupported source language '{descriptor.SourceLanguage}'"));
        }

        diagnostics.AddRange(descriptor.RequiredHostCapabilities
            .Where(capability => !available.Contains(capability))
            .Select(capability => DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.MissingHostCapability,
                descriptor.Identity.Id,
                $"requires unavailable host capability '{capability}'")));
        return diagnostics.ToImmutable();
    }

    static GenerationDiagnostic DescriptorDiagnostic(
        AdapterDescriptor descriptor,
        AdapterContributionAdmissionDiagnostic diagnostic) =>
        DotNetAdapterDiagnostics.Error(
            DotNetAdapterGenerationDiagnosticCodes.DescriptorRejected,
            descriptor.Identity.Id,
            $"descriptor was rejected with admission code '{diagnostic.Code}' at '{diagnostic.Path}'");

    static IEnumerable<GenerationDiagnostic> DiagnosticsFrom(IEnumerable<AdapterRunRecord> records)
    {
        foreach (var record in records)
        {
            foreach (var diagnostic in record.Probe is AdapterProbeBlocked blocked ? blocked.Diagnostics : [])
            {
                yield return diagnostic;
            }

            foreach (var diagnostic in record.Execution.Diagnostics)
            {
                yield return diagnostic;
            }
        }
    }

    static AdapterDescriptor FallbackDescriptor() => new()
    {
        Identity = new AdapterIdentity { Id = "runner:descriptor-failure", Version = "unavailable" },
        SourceLanguage = AdapterSourceLanguage.SourceIndependent,
        Category = AdapterCategory.Legacy
    };

    static Version CurrentGenerationContractsVersion() =>
        typeof(AdapterDescriptor).Assembly.GetName().Version ?? new Version(0, 0);

    sealed record PreparedRegistration(
        DotNetAdapterRegistration Registration,
        AdapterDescriptor Descriptor,
        ImmutableArray<AdapterContributionAdmissionDiagnostic> DescriptorDiagnostics,
        GenerationDiagnostic? Failure);

    sealed class PreparedRegistrationComparer : IComparer<PreparedRegistration>
    {
        public static PreparedRegistrationComparer Instance { get; } = new();

        public int Compare(PreparedRegistration? x, PreparedRegistration? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            var descriptorComparison = Compare(x.Descriptor, y.Descriptor);
            if (descriptorComparison != 0)
            {
                return descriptorComparison;
            }

            return StringComparer.Ordinal.Compare(x.Failure?.Message, y.Failure?.Message);
        }

        static int Compare(AdapterDescriptor left, AdapterDescriptor right)
        {
            var comparison = StringComparer.Ordinal.Compare(left.Identity.Id, right.Identity.Id);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = StringComparer.Ordinal.Compare(left.Identity.Version, right.Identity.Version);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.SourceLanguage.CompareTo(right.SourceLanguage);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = left.Category.CompareTo(right.Category);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareMinimumVersion(
                left.CompatibleGenerationVersions.MinimumInclusive,
                right.CompatibleGenerationVersions.MinimumInclusive);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareMaximumVersion(
                left.CompatibleGenerationVersions.MaximumExclusive,
                right.CompatibleGenerationVersions.MaximumExclusive);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareEnums(left.RequiredHostCapabilities, right.RequiredHostCapabilities);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareApiCapabilities(left.RequiredApiCapabilities, right.RequiredApiCapabilities);
            return comparison != 0
                ? comparison
                : CompareEnums(left.EmittedFactCapabilities, right.EmittedFactCapabilities);
        }

        static int CompareMinimumVersion(Version? left, Version? right)
        {
            if (left is null)
            {
                return right is null ? 0 : -1;
            }

            return right is null ? 1 : left.CompareTo(right);
        }

        static int CompareMaximumVersion(Version? left, Version? right)
        {
            if (left is null)
            {
                return right is null ? 0 : 1;
            }

            return right is null ? -1 : left.CompareTo(right);
        }

        static int CompareEnums<T>(ImmutableArray<T> left, ImmutableArray<T> right)
            where T : struct, Enum
        {
            var count = Math.Min(left.Length, right.Length);
            for (var index = 0; index < count; index++)
            {
                var comparison = Comparer<T>.Default.Compare(left[index], right[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }

        static int CompareApiCapabilities(
            ImmutableArray<AdapterApiCapability> left,
            ImmutableArray<AdapterApiCapability> right)
        {
            var count = Math.Min(left.Length, right.Length);
            for (var index = 0; index < count; index++)
            {
                var comparison = StringComparer.Ordinal.Compare(left[index].Id, right[index].Id);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
