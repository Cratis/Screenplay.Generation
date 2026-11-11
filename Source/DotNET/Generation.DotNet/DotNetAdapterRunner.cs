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
        var rosterFacts = new RosterFacts(projectDiagnostics, hostCapabilities, generationContractsVersion);
        var inputs = new AnalysisInputs(context, options);
        var records = prepared
            .Select(item => Run(
                item,
                duplicateIds.Contains(item.Descriptor.Identity.Id),
                rosterFacts,
                inputs))
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
        RosterFacts facts,
        AnalysisInputs inputs)
    {
        if (item.Failure is not null)
        {
            return BeforeProbe(item.Descriptor, null, [item.Failure], AdapterRunDisposition.RosterRejected);
        }

        if (isDuplicate)
        {
            var diagnostic = DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.DuplicateAdapterId,
                item.Descriptor.Identity.Id,
                "was rejected because its identity occurs more than once in the roster");
            return BeforeProbe(item.Descriptor, null, [diagnostic], AdapterRunDisposition.RosterRejected);
        }

        if (!item.DescriptorDiagnostics.IsEmpty)
        {
            return BeforeProbe(
                item.Descriptor,
                null,
                [.. item.DescriptorDiagnostics.Select(diagnostic => DescriptorDiagnostic(item.Descriptor, diagnostic))],
                AdapterRunDisposition.RosterRejected);
        }

        if (!facts.ProjectDiagnostics.IsEmpty && RequiresProjectRoster(item.Descriptor))
        {
            return BlockedBeforeProbe(item.Descriptor, facts.ProjectDiagnostics);
        }

        var compatibilityDiagnostics = GenerationCompatibilityDiagnostics(item.Descriptor, facts.GenerationContractsVersion);
        if (!compatibilityDiagnostics.IsEmpty)
        {
            return BlockedBeforeProbe(item.Descriptor, compatibilityDiagnostics);
        }

        var hostDiagnostics = HostDiagnostics(item.Descriptor, facts.HostCapabilities);
        if (!hostDiagnostics.IsEmpty)
        {
            return BlockedBeforeProbe(item.Descriptor, hostDiagnostics);
        }

        return ProbeAndRun(item, inputs);
    }

    static AdapterRunRecord ProbeAndRun(PreparedRegistration item, AnalysisInputs inputs)
    {
        var context = inputs.Context;
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
            AdapterProbeApplicable => Analyze(item, probe, sourceValidator, inputs),
            _ => BlockedAfterProbe(
                item.Descriptor,
                DotNetAdapterProbeAdmission.Malformed(item.Descriptor.Identity.Id))
        };
    }

    static AdapterRunRecord Analyze(
        PreparedRegistration item,
        AdapterProbeResult probe,
        ISourceAuthorityValidator sourceValidator,
        AnalysisInputs inputs)
    {
        AdapterContribution contribution;
        try
        {
            contribution = item.Registration.Analyze(inputs.Context, inputs.Options);
        }
        catch (Exception exception)
        {
            return ExecutionFailed(item, probe, "Analyze", exception);
        }

        AdapterContributionAdmissionResult admission;
        try
        {
            admission = AdapterContributionAdmission.Admit(item.Descriptor, contribution, sourceValidator);
        }
        catch (Exception exception)
        {
            return ExecutionFailed(item, probe, "ContributionAdmission", exception);
        }

        if (!admission.IsAdmitted)
        {
            var diagnostic = DotNetAdapterDiagnostics.Error(
                DotNetAdapterGenerationDiagnosticCodes.ContributionRejected,
                item.Descriptor.Identity.Id,
                "contribution was rejected atomically");
            return Executed(
                item,
                probe,
                new AdapterExecutionRejected
                {
                    Diagnostics = [diagnostic],
                    AdmissionDiagnostics = admission.Diagnostics
                },
                AdapterRunDisposition.ContributionRejected);
        }

        var snapshot = admission.Snapshot!;
        return Executed(
            item,
            probe,
            new AdapterExecutionCompleted
            {
                Diagnostics = snapshot.Diagnostics,
                Contribution = snapshot
            },
            AdapterRunDisposition.Admitted);
    }

    static AdapterRunRecord ExecutionFailed(
        PreparedRegistration item,
        AdapterProbeResult probe,
        string operation,
        Exception exception)
    {
        var diagnostic = DotNetAdapterDiagnostics.OperationFailed(item.Descriptor.Identity.Id, operation, exception);
        return Executed(item, probe, new AdapterExecutionFailed { Diagnostics = [diagnostic] }, AdapterRunDisposition.ExecutionFailed);
    }

    static AdapterRunRecord Executed(
        PreparedRegistration item,
        AdapterProbeResult probe,
        AdapterExecutionResult execution,
        AdapterRunDisposition disposition) => new()
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = item.Descriptor,
            Probe = probe,
            Execution = execution,
            Disposition = disposition
        };

    static AdapterRunRecord BlockedBeforeProbe(
        AdapterDescriptor descriptor,
        ImmutableArray<GenerationDiagnostic> diagnostics) =>
        BeforeProbe(descriptor, new AdapterProbeBlocked { Diagnostics = diagnostics }, diagnostics, AdapterRunDisposition.Blocked);

    static AdapterRunRecord BeforeProbe(
        AdapterDescriptor descriptor,
        AdapterProbeResult? probe,
        ImmutableArray<GenerationDiagnostic> diagnostics,
        AdapterRunDisposition disposition) => new()
        {
            Considered = true,
            Descriptor = descriptor,
            Probe = probe ?? new AdapterProbeNotRun(),
            Execution = new AdapterExecutionNotRun { Diagnostics = diagnostics },
            Disposition = disposition
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
        if (IsCompatible(range, generationContractsVersion))
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

    static bool IsCompatible(GenerationVersionRange range, Version version) =>
        version.CompareTo(range.MinimumInclusive) >= 0 &&
        (range.MaximumExclusive is null || version.CompareTo(range.MaximumExclusive) < 0);

    static bool IsPortableRelativeProjectPath(string? path) =>
        !string.IsNullOrEmpty(path) &&
        HasPortableShape(path) &&
        IsNormalized(path) &&
        path.Split('/').Select((segment, index) => IsPortableSegment(segment, index == 0)).All(portable => portable);

    static bool HasPortableShape(string path) =>
        string.Equals(path, path.Trim(), StringComparison.Ordinal) &&
        !path.Any(char.IsControl) &&
        path[0] != '/' &&
        !path.Contains('\\') &&
        !IsDriveRooted(path);

    static bool IsDriveRooted(string value) =>
        value.Length >= 2 && char.IsAsciiLetter(value[0]) && value[1] == ':';

    static bool IsNormalized(string path)
    {
        try
        {
            return string.Equals(path, path.Normalize(), StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static bool IsPortableSegment(string segment, bool isFirst)
    {
        if (string.IsNullOrEmpty(segment) || !TryDecodeProjectPathSegment(segment, out var decoded))
        {
            return false;
        }

        return !string.Equals(decoded, ".", StringComparison.Ordinal) &&
            !string.Equals(decoded, "..", StringComparison.Ordinal) &&
            !decoded.Contains('/') &&
            !decoded.Contains('\\') &&
            !(isFirst && IsDriveRooted(decoded));
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

    sealed record RosterFacts(
        ImmutableArray<GenerationDiagnostic> ProjectDiagnostics,
        ImmutableArray<AdapterHostCapability> HostCapabilities,
        Version GenerationContractsVersion);

    sealed record AnalysisInputs(DotNetAnalysisContext Context, DotNetAdapterOptions Options);

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
            var comparison = CompareIdentity(left, right);
            return comparison != 0 ? comparison : CompareRequirements(left, right);
        }

        static int CompareIdentity(AdapterDescriptor left, AdapterDescriptor right)
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

            return CompareMaximumVersion(
                left.CompatibleGenerationVersions.MaximumExclusive,
                right.CompatibleGenerationVersions.MaximumExclusive);
        }

        static int CompareRequirements(AdapterDescriptor left, AdapterDescriptor right)
        {
            var comparison = CompareSequences(left.RequiredHostCapabilities, right.RequiredHostCapabilities, Comparer<AdapterHostCapability>.Default.Compare);
            if (comparison != 0)
            {
                return comparison;
            }

            comparison = CompareSequences(
                left.RequiredApiCapabilities,
                right.RequiredApiCapabilities,
                (a, b) => StringComparer.Ordinal.Compare(a.Id, b.Id));
            return comparison != 0
                ? comparison
                : CompareSequences(left.EmittedFactCapabilities, right.EmittedFactCapabilities, Comparer<GenerationFactCapability>.Default.Compare);
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

        static int CompareSequences<T>(ImmutableArray<T> left, ImmutableArray<T> right, Comparison<T> compareItems)
        {
            var count = Math.Min(left.Length, right.Length);
            for (var index = 0; index < count; index++)
            {
                var comparison = compareItems(left[index], right[index]);
                if (comparison != 0)
                {
                    return comparison;
                }
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
