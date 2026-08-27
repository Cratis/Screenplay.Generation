// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_canonicalizing_complete_adapter_run_snapshots : given.a_generator
{
    readonly AdapterIdentity _completedAdapter = new() { Id = "adapter-completed", Version = "2.0.0" };
    readonly AdapterIdentity _rejectedAdapter = new() { Id = "adapter-rejected", Version = "3.0.0" };
    readonly List<string> _features = ["Registration", "Opening"];
    readonly List<PropertyDefinition> _properties = [];
    AdapterRunRecord _completedInput = null!;
    GeneratedScreenplayDefinition _forward = null!;
    GeneratedScreenplayDefinition _reverse = null!;

    void Because()
    {
        var subject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" };
        var artifact = new ArtifactKey { Subject = subject, Kind = ArtifactKind.Event };
        _properties.Add(new PropertyDefinition
        {
            Name = "accountId",
            Type = new TypeReferenceDefinition
            {
                Name = "AccountId",
                Subject = new SubjectId { Value = "dotnet://Banking/Concepts.AccountId" },
                IsOptional = true
            },
            IsIdentifier = true
        });
        var facts = new GenerationFact[]
        {
            new ArtifactPlacementFact
            {
                Id = new FactId { Value = "adapter-completed:placement" },
                Subject = subject,
                Evidence = FactEvidence(),
                Artifact = artifact,
                Placement = new ArtifactPlacement
                {
                    Module = "Accounts",
                    Features = _features,
                    Slice = "Open",
                    SliceKind = GenerationSliceKind.StateChange
                }
            },
            new ArtifactFact
            {
                Id = new FactId { Value = "adapter-completed:artifact" },
                Subject = subject,
                Evidence = FactEvidence(),
                Definition = new ArtifactDefinition
                {
                    Key = artifact,
                    Name = "AccountOpened",
                    Description = "An account was opened",
                    File = "Accounts/Open/AccountOpened.cs",
                    Properties = _properties
                }
            }
        };
        var contributionDiagnostics = GenerationDiagnostics("CONTRIBUTION", subject);
        var executionDiagnostics = GenerationDiagnostics("EXECUTION", subject);
        _completedInput = CompletedRecord(
            facts,
            contributionDiagnostics,
            executionDiagnostics,
            reverse: false);
        var completedReverse = CompletedRecord(
            Reversed(facts),
            Reversed(contributionDiagnostics),
            Reversed(executionDiagnostics),
            reverse: true);
        var rejected = RejectedRecord(reverse: false);
        var rejectedReverse = RejectedRecord(reverse: true);
        var topDiagnostics = GenerationDiagnostics("TOP", subject);
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        _forward = Generator.Generate(
            Snapshot(_completedInput, rejected) with { Diagnostics = [.. topDiagnostics] },
            options);
        _reverse = Generator.Generate(
            Snapshot(rejectedReverse, completedReverse) with
            {
                Diagnostics = [.. Reversed(topDiagnostics)]
            },
            options);

        _features.Add("Mutated");
        _properties.Clear();
    }

    [Fact] void should_return_recursively_identical_canonical_adapter_runs() => AdapterRunProjection(_reverse.AdapterRun).ShouldEqual(AdapterRunProjection(_forward.AdapterRun));
    [Fact] void should_return_a_new_adapter_record() => ReferenceEquals(_forward.AdapterRun!.Adapters[0], _completedInput).ShouldBeFalse();
    [Fact] void should_deep_clone_the_descriptor() => ReferenceEquals(_forward.AdapterRun!.Adapters[0].Descriptor, _completedInput.Descriptor).ShouldBeFalse();
    [Fact] void should_canonicalize_host_capabilities() => _forward.AdapterRun!.Adapters[0].Descriptor.RequiredHostCapabilities.ShouldContainOnly(AdapterHostCapability.AuthoredSource, AdapterHostCapability.SemanticAnalysis);
    [Fact] void should_canonicalize_api_capabilities() => _forward.AdapterRun!.Adapters[0].Descriptor.RequiredApiCapabilities.Select(_ => _.Id).ShouldEqual(["api.alpha", "api.zeta"]);
    [Fact] void should_canonicalize_fact_capabilities() => _forward.AdapterRun!.Adapters[0].Descriptor.EmittedFactCapabilities.ShouldContainOnly(GenerationFactCapability.Artifact, GenerationFactCapability.ArtifactPlacement);
    [Fact] void should_canonicalize_probe_evidence() => _forward.AdapterRun!.Adapters[0].Probe.Evidence.Select(_ => _.Description).ShouldEqual(["Subject", "API"]);
    [Fact] void should_preserve_the_complete_probe_source_range() => _forward.AdapterRun!.Adapters[0].Probe.Evidence[0].Source!.EndColumn.ShouldEqual(9);
    [Fact] void should_canonicalize_completed_contribution_facts() => CompletedContribution().Facts.Select(_ => _.Id.Value).ShouldEqual(["adapter-completed:artifact", "adapter-completed:placement"]);
    [Fact] void should_preserve_authored_property_order_by_deep_clone() => ((ArtifactFact)CompletedContribution().Facts[0]).Definition.Properties.Select(_ => _.Name).ShouldEqual(["accountId"]);
    [Fact] void should_preserve_authored_feature_order_by_deep_clone() => ((ArtifactPlacementFact)CompletedContribution().Facts[1]).Placement.Features.ShouldEqual("Registration", "Opening");
    [Fact] void should_preserve_completed_contribution_diagnostics() => CompletedContribution().Diagnostics.Select(_ => _.Message).ShouldContainOnly("alpha", "zeta");
    [Fact] void should_canonicalize_rejected_admission_diagnostics() => RejectedExecution().AdmissionDiagnostics[0].Path.ShouldEqual("z.path");
    [Fact] void should_deep_clone_fact_records() => ReferenceEquals(_forward.AdapterRun!.Facts[0].Fact, CompletedContribution().Facts[0]).ShouldBeFalse();
    [Fact] void should_deep_clone_run_diagnostics() => ReferenceEquals(_forward.AdapterRun!.Diagnostics[0], _completedInput.Execution.Diagnostics[0]).ShouldBeFalse();

    AdapterRunRecord CompletedRecord(
        IReadOnlyList<GenerationFact> facts,
        IReadOnlyList<GenerationDiagnostic> contributionDiagnostics,
        IReadOnlyList<GenerationDiagnostic> executionDiagnostics,
        bool reverse)
    {
        var descriptor = Descriptor(_completedAdapter, reverse);
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeApplicable
            {
                Evidence = ProbeEvidence(reverse)
            },
            Execution = new AdapterExecutionCompleted
            {
                Diagnostics = [.. executionDiagnostics],
                Contribution = new AdapterContributionSnapshot
                {
                    Descriptor = descriptor,
                    Facts = [.. facts],
                    Diagnostics = [.. contributionDiagnostics]
                }
            },
            Disposition = AdapterRunDisposition.Admitted
        };
    }

    AdapterRunRecord RejectedRecord(bool reverse)
    {
        var descriptor = Descriptor(_rejectedAdapter, reverse);
        var diagnostics = new[]
        {
            new AdapterContributionAdmissionDiagnostic
            {
                Code = AdapterContributionAdmissionDiagnosticCode.InvalidFactId,
                Path = "z.path",
                Message = "zeta",
                Fact = new FactId { Value = "adapter-rejected:zeta" },
                Subject = new SubjectId { Value = "dotnet://Banking/Zeta" },
                Source = Source("Rejected/Zeta.cs", 7)
            },
            new AdapterContributionAdmissionDiagnostic
            {
                Code = AdapterContributionAdmissionDiagnosticCode.InvalidSubject,
                Path = "a.path",
                Message = "alpha",
                Fact = new FactId { Value = "adapter-rejected:alpha" },
                Subject = new SubjectId { Value = "dotnet://Banking/Alpha" },
                Source = Source("Rejected/Alpha.cs", 3)
            }
        };
        return new AdapterRunRecord
        {
            Considered = true,
            Probed = true,
            Executed = true,
            Descriptor = descriptor,
            Probe = new AdapterProbeApplicable { Evidence = ProbeEvidence(reverse) },
            Execution = new AdapterExecutionRejected
            {
                Diagnostics = [.. GenerationDiagnostics("REJECTED", diagnostics[0].Subject!)],
                AdmissionDiagnostics = reverse
                    ? [.. Reversed(diagnostics)]
                    : [.. diagnostics]
            },
            Disposition = AdapterRunDisposition.ContributionRejected
        };
    }

    AdapterDescriptor Descriptor(AdapterIdentity identity, bool reverse)
    {
        var host = new[] { AdapterHostCapability.SemanticAnalysis, AdapterHostCapability.AuthoredSource };
        var api = new[] { new AdapterApiCapability { Id = "api.zeta" }, new AdapterApiCapability { Id = "api.alpha" } };
        var facts = new[] { GenerationFactCapability.ArtifactPlacement, GenerationFactCapability.Artifact };
        return new AdapterDescriptor
        {
            Identity = identity,
            SourceLanguage = AdapterSourceLanguage.CSharp,
            Category = AdapterCategory.EventSourcing,
            CompatibleGenerationVersions = new GenerationVersionRange
            {
                MinimumInclusive = new Version(1, 2, 3),
                MaximumExclusive = new Version(4, 5, 6)
            },
            RequiredHostCapabilities = Values(host, reverse),
            RequiredApiCapabilities = Values(api, reverse),
            EmittedFactCapabilities = Values(facts, reverse)
        };
    }

    ImmutableArray<AdapterProbeEvidence> ProbeEvidence(bool reverse)
    {
        var evidence = new[]
        {
            new AdapterProbeEvidence
            {
                Description = "Subject",
                Subject = new SubjectId { Value = "dotnet://Banking/Events.AccountOpened" },
                Source = Source("Probe/Subject.cs", 5)
            },
            new AdapterProbeEvidence
            {
                Description = "API",
                ApiCapability = new AdapterApiCapability { Id = "api.alpha" },
                Source = Source("Probe/Api.cs", 2)
            }
        };
        return reverse ? [.. Reversed(evidence)] : [.. evidence];
    }

    Evidence FactEvidence() => new()
    {
        Adapter = _completedAdapter,
        Strength = EvidenceStrength.Exact,
        Explanation = "Authored declaration",
        Source = Source("Accounts/Open/AccountOpened.cs", 11)
    };

    static GenerationDiagnostic[] GenerationDiagnostics(string code, SubjectId subject) =>
    [
        new GenerationDiagnostic
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Information,
            Message = "zeta",
            Source = Source("Diagnostics/Zeta.cs", 8),
            Subject = subject
        },
        new GenerationDiagnostic
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Information,
            Message = "alpha",
            Source = Source("Diagnostics/Alpha.cs", 4),
            Subject = subject
        }
    ];

    static SourceRange Source(string path, int start) => new()
    {
        Path = path,
        FileIdentity = new SourceFileIdentity { Project = "Banking", Path = path },
        StartLine = start,
        StartColumn = 2,
        EndLine = start + 1,
        EndColumn = 9
    };

    static ImmutableArray<T> Values<T>(IEnumerable<T> values, bool reverse) =>
        reverse ? [.. Reversed(values)] : [.. values];

    static T[] Reversed<T>(IEnumerable<T> values)
    {
        var reversed = values.ToArray();
        Array.Reverse(reversed);
        return reversed;
    }

    AdapterContributionSnapshot CompletedContribution() =>
        ((AdapterExecutionCompleted)_forward.AdapterRun!.Adapters.Single(_ => _.Descriptor.Identity == _completedAdapter).Execution).Contribution;

    AdapterExecutionRejected RejectedExecution() =>
        (AdapterExecutionRejected)_forward.AdapterRun!.Adapters.Single(_ => _.Descriptor.Identity == _rejectedAdapter).Execution;
}
