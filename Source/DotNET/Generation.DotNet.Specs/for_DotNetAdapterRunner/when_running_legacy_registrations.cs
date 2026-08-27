// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_running_legacy_registrations : given.a_runner_context
{
    LegacyAdapter _applicable = null!;
    LegacyAdapter _notApplicable = null!;
    AdapterRunSnapshot _snapshot = null!;
    string _directFactId = null!;
    AdapterRunRecord _unscopedRecord = null!;

    void Establish()
    {
        var applicableIdentity = new AdapterIdentity { Id = "legacy-scoped", Version = "1.0.0" };
        _applicable = new(applicableIdentity)
        {
            Contribution = ArtifactContribution(applicableIdentity, LegacyRange())
        };
        _notApplicable = new(new AdapterIdentity { Id = "legacy-not-applicable", Version = "1.0.0" })
        {
            IsApplicable = false
        };
    }

    void Because()
    {
        var project = LegacyProject();
        _snapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(_notApplicable), DotNetAdapterRegistration.ForLegacy(_applicable)],
            new DotNetAnalysisContext([project]),
            Options);

        var unscopedIdentity = new AdapterIdentity { Id = "legacy-unscoped", Version = "1.0.0" };
        var unscoped = new LegacyAdapter(unscopedIdentity)
        {
            Contribution = ArtifactContribution(unscopedIdentity, LegacyRange(), "artifact")
        };
        _directFactId = unscoped.Analyze(new DotNetAnalysisContext([project]), Options).Facts.Single().Id.Value;
        _unscopedRecord = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.ForLegacy(unscoped)],
            new DotNetAnalysisContext([project]),
            Options).Adapters.Single();
    }

    [Fact] void should_call_legacy_can_analyze_once() => (_applicable.CanAnalyzeCount + _notApplicable.CanAnalyzeCount).ShouldEqual(2);
    [Fact] void should_execute_only_the_applicable_legacy_adapter_once() => _applicable.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_not_execute_the_not_applicable_legacy_adapter() => _notApplicable.AnalyzeCount.ShouldEqual(0);
    [Fact] void should_admit_a_scoped_official_legacy_contribution() => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "legacy-scoped").Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_use_the_legacy_compatibility_descriptor() => _snapshot.Adapters.Single(record => record.Descriptor.Identity.Id == "legacy-scoped").Descriptor.Category.ShouldEqual(AdapterCategory.Legacy);
    [Fact] void should_accept_exact_legacy_path_only_source() => _snapshot.Facts.Length.ShouldEqual(1);
    [Fact] void should_reject_unscoped_fact_ids_only_at_the_runner_admission_boundary() => _unscopedRecord.Disposition.ShouldEqual(AdapterRunDisposition.ContributionRejected);
    [Fact] void should_leave_direct_legacy_analysis_behavior_unchanged() => _directFactId.ShouldEqual("artifact");

    static SourceRange LegacyRange() => new()
    {
        Path = "Code.cs",
        StartLine = 1,
        StartColumn = 1,
        EndLine = 1,
        EndColumn = 7
    };

    static DotNetProjectCompilation LegacyProject()
    {
        var tree = CSharpSyntaxTree.ParseText("public class Legacy { }\n", path: "/workspace/Code.cs");
        return new DotNetProjectCompilation
        {
            Name = "Legacy.Project",
            SourceRoot = "/workspace",
            Compilation = CSharpCompilation.Create("Legacy", [tree], CompilationFrom().References),
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
    }
}
