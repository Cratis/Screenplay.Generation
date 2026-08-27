// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_project_roster_has_duplicate_stable_identities : given.a_runner_context
{
    ModernAdapter _forwardAdapter = null!;
    ModernAdapter _reverseAdapter = null!;
    ModernAdapter _forwardIndependent = null!;
    ModernAdapter _reverseIndependent = null!;
    AdapterRunSnapshot _forward = null!;
    AdapterRunSnapshot _reverse = null!;

    void Because()
    {
        var first = MappedProject(
            "duplicate-project",
            "First.Project",
            "First",
            "/checkout/first/Code.cs",
            "public class First;");
        var second = MappedProject(
            "duplicate-project",
            "Second.Project",
            "Second",
            "/different/second/Code.cs",
            "public class Second;");
        _forwardAdapter = new ModernAdapter(Descriptor("adapter", language: AdapterSourceLanguage.CSharp));
        _reverseAdapter = new ModernAdapter(Descriptor("adapter", language: AdapterSourceLanguage.CSharp));
        _forwardIndependent = new ModernAdapter(Descriptor("independent"));
        _reverseIndependent = new ModernAdapter(Descriptor("independent"));
        _forward = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(_forwardAdapter), DotNetAdapterRegistration.For(_forwardIndependent)],
            new DotNetAnalysisContext([first, second]),
            Options);
        _reverse = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(_reverseIndependent), DotNetAdapterRegistration.For(_reverseAdapter)],
            new DotNetAnalysisContext([second, first]),
            Options);
    }

    [Fact] void should_read_the_descriptor_without_probing_or_executing_the_source_adapter() => (_forwardAdapter.DescriptorCount, _forwardAdapter.ProbeCount, _forwardAdapter.AnalyzeCount).ShouldEqual((1, 0, 0));
    [Fact] void should_record_the_source_adapter_as_considered_and_blocked() => SourceRecord().Considered.ShouldBeTrue();
    [Fact] void should_not_probe_the_source_adapter() => SourceRecord().Probed.ShouldBeFalse();
    [Fact] void should_not_execute_the_source_adapter() => SourceRecord().Executed.ShouldBeFalse();
    [Fact] void should_block_the_source_adapter() => SourceRecord().Disposition.ShouldEqual(AdapterRunDisposition.Blocked);
    [Fact] void should_continue_to_execute_an_unrelated_source_independent_adapter() => (_forwardIndependent.DescriptorCount, _forwardIndependent.ProbeCount, _forwardIndependent.AnalyzeCount).ShouldEqual((1, 1, 1));
    [Fact] void should_admit_the_unrelated_source_independent_adapter() => _forward.Adapters.Single(record => record.Descriptor.Identity.Id == "independent").Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_report_the_stable_host_project_roster_diagnostic() => Projection(_forward).ShouldEqual("DOTNETADAPTER010:The .NET adapter host rejected the project roster because stable project identity 'duplicate-project' occurs more than once");
    [Fact] void should_keep_the_diagnostic_identical_under_reversed_project_input() => Projection(_reverse).ShouldEqual(Projection(_forward));
    [Fact] void should_preserve_the_reversed_source_adapter_record() => (_reverseAdapter.DescriptorCount, _reverseAdapter.ProbeCount, _reverseAdapter.AnalyzeCount).ShouldEqual((1, 0, 0));
    [Fact] void should_continue_to_execute_the_reversed_source_independent_adapter() => (_reverseIndependent.DescriptorCount, _reverseIndependent.ProbeCount, _reverseIndependent.AnalyzeCount).ShouldEqual((1, 1, 1));

    AdapterRunRecord SourceRecord() => _forward.Adapters.Single(record => record.Descriptor.Identity.Id == "adapter");

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));
}
