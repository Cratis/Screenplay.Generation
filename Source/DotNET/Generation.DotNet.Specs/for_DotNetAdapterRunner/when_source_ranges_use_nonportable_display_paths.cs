// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_source_ranges_use_nonportable_display_paths : given.a_runner_context
{
    static readonly string[] _invalidPaths =
    [
        "/checkout/Code.cs",
        "C:/checkout/Code.cs",
        "Folder\\Code.cs",
        "../Code.cs",
        "./Code.cs",
        "%2e/Code.cs",
        "%2e%2e/Code.cs"
    ];

    AdapterRunRecord[] _modernProbeRecords = null!;
    AdapterRunRecord[] _legacyAuthorityProbeRecords = null!;
    AdapterRunRecord[] _modernContributionRecords = null!;
    AdapterRunRecord[] _legacyContributionRecords = null!;
    AdapterRunRecord[] _validRecords = null!;

    void Because()
    {
        var mappedProject = MappedProject(
            "project",
            "Project",
            "Project",
            "/checkout/Project/Code.cs",
            "public class Code { }\n");
        var mappedFile = mappedProject.SourceContext!.Files.Values.Single();
        var legacyProject = LegacyProject();

        _modernProbeRecords =
        [
            .. _invalidPaths.Select((path, index) => RunModernProbe(mappedProject, mappedFile, path, index))
        ];
        _legacyAuthorityProbeRecords =
        [
            .. _invalidPaths.Select((path, index) => RunLegacyAuthorityProbe(legacyProject, path, index))
        ];
        _modernContributionRecords =
        [
            .. _invalidPaths.Select((path, index) => RunModernContribution(mappedProject, mappedFile, path, index))
        ];
        _legacyContributionRecords =
        [
            .. _invalidPaths.Select((path, index) => RunLegacyContribution(legacyProject, path, index))
        ];
        _validRecords =
        [
            RunModernProbe(mappedProject, mappedFile, "Code.cs", 100),
            RunLegacyAuthorityProbe(legacyProject, "Code.cs", 100),
            RunModernContribution(mappedProject, mappedFile, "Code.cs", 100),
            RunLegacyContribution(legacyProject, "Code.cs", 100)
        ];
    }

    [Fact] void should_reject_every_nonportable_modern_probe_path_as_malformed() => _modernProbeRecords.All(IsRejectedProbe).ShouldBeTrue();
    [Fact] void should_reject_every_nonportable_path_through_the_legacy_authority_map() => _legacyAuthorityProbeRecords.All(IsRejectedProbe).ShouldBeTrue();
    [Fact] void should_reject_every_nonportable_modern_contribution_path_structurally() => _modernContributionRecords.All(HasInvalidSourceRangeAdmission).ShouldBeTrue();
    [Fact] void should_reject_every_nonportable_legacy_contribution_path_structurally() => _legacyContributionRecords.All(HasInvalidSourceRangeAdmission).ShouldBeTrue();
    [Fact] void should_continue_to_admit_normalized_relative_probe_and_contribution_paths() => _validRecords.All(record => record.Disposition == AdapterRunDisposition.Admitted).ShouldBeTrue();

    static AdapterRunRecord RunModernProbe(
        DotNetProjectCompilation project,
        DotNetSourceFile file,
        string path,
        int index)
    {
        var descriptor = SourceDescriptor($"modern-probe-{index}", stable: true);
        var adapter = new ModernAdapter(descriptor)
        {
            ProbeResult = new AdapterProbeApplicable
            {
                Evidence =
                [
                    new AdapterProbeEvidence
                    {
                        Description = "The exact API is present",
                        Source = Range(path, file.Identity)
                    }
                ]
            }
        };
        return Run(adapter, project);
    }

    static AdapterRunRecord RunLegacyAuthorityProbe(
        DotNetProjectCompilation project,
        string path,
        int index)
    {
        var descriptor = SourceDescriptor($"legacy-authority-probe-{index}", stable: false);
        var adapter = new ModernAdapter(descriptor)
        {
            ProbeResult = new AdapterProbeApplicable
            {
                Evidence =
                [
                    new AdapterProbeEvidence
                    {
                        Description = "The exact API is present",
                        Source = Range(path, null)
                    }
                ]
            }
        };
        return Run(adapter, project);
    }

    static AdapterRunRecord RunModernContribution(
        DotNetProjectCompilation project,
        DotNetSourceFile file,
        string path,
        int index)
    {
        var descriptor = SourceDescriptor(
            $"modern-contribution-{index}",
            stable: true,
            factCapabilities: [GenerationFactCapability.Artifact]);
        var adapter = new ModernAdapter(descriptor)
        {
            Contribution = ArtifactContribution(descriptor.Identity, Range(path, file.Identity))
        };
        return Run(adapter, project);
    }

    static AdapterRunRecord RunLegacyContribution(
        DotNetProjectCompilation project,
        string path,
        int index)
    {
        var identity = new AdapterIdentity { Id = $"legacy-contribution-{index}", Version = "1.0.0" };
        var adapter = new LegacyAdapter(identity)
        {
            Contribution = ArtifactContribution(identity, Range(path, null))
        };
        return DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(adapter)],
            new DotNetAnalysisContext([project]),
            Options).Adapters.Single();
    }

    static AdapterDescriptor SourceDescriptor(
        string id,
        bool stable,
        System.Collections.Immutable.ImmutableArray<GenerationFactCapability> factCapabilities = default) =>
        Descriptor(
            id,
            language: AdapterSourceLanguage.CSharp,
            hostCapabilities: stable
                ?
                [
                    AdapterHostCapability.AuthoredSource,
                    AdapterHostCapability.StableSourceLocations,
                    AdapterHostCapability.SemanticAnalysis
                ]
                :
                [
                    AdapterHostCapability.AuthoredSource,
                    AdapterHostCapability.SemanticAnalysis
                ],
            factCapabilities: factCapabilities);

    static AdapterRunRecord Run(ModernAdapter adapter, DotNetProjectCompilation project) =>
        DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext([project]),
            Options).Adapters.Single();

    static bool IsRejectedProbe(AdapterRunRecord record) =>
        record.Disposition == AdapterRunDisposition.Blocked &&
        ((AdapterProbeBlocked)record.Probe).Diagnostics.Single().Code == DotNetAdapterGenerationDiagnosticCodes.ProbeRejected;

    static bool HasInvalidSourceRangeAdmission(AdapterRunRecord record) =>
        record.Execution is AdapterExecutionRejected rejected &&
        rejected.AdmissionDiagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidSourceRange);

    static SourceRange Range(string path, SourceFileIdentity? identity) => new()
    {
        Path = path,
        FileIdentity = identity,
        StartLine = 1,
        StartColumn = 1,
        EndLine = 1,
        EndColumn = 7
    };

    static DotNetProjectCompilation LegacyProject()
    {
        var tree = CSharpSyntaxTree.ParseText(
            "public class Code { }\n",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            "/checkout/Project/Code.cs");
        return new DotNetProjectCompilation
        {
            Name = "Project",
            SourceRoot = "/checkout/Project",
            Compilation = CSharpCompilation.Create(
                "Project",
                [tree],
                CompilationFrom().References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)),
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
    }
}
