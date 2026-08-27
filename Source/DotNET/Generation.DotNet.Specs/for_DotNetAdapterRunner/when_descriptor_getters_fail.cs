// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_descriptor_getters_fail : given.a_runner_context
{
    ModernAdapter[] _forwardAdapters = null!;
    ModernAdapter[] _reverseAdapters = null!;
    ModernAdapter _forwardValid = null!;
    ModernAdapter _reverseValid = null!;
    AdapterRunSnapshot _forward = null!;
    AdapterRunSnapshot _reverse = null!;

    void Because()
    {
        (_forwardAdapters, _forwardValid) = Adapters();
        (_reverseAdapters, _reverseValid) = Adapters();
        _forward = DotNetAdapterRunner.Run(
            _forwardAdapters.Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options);
        _reverse = DotNetAdapterRunner.Run(
            _reverseAdapters.AsEnumerable().Reverse().Select(DotNetAdapterRegistration.For),
            new DotNetAnalysisContext([]),
            Options);
    }

    [Fact] void should_handle_every_descriptor_failure_before_duplicate_grouping() => Failed(_forward).All(record => record.Execution.Diagnostics.Single().Code == DotNetAdapterGenerationDiagnosticCodes.OperationFailed).ShouldBeTrue();
    [Fact] void should_record_each_descriptor_failure() => Failed(_forward).Length.ShouldEqual(2);
    [Fact] void should_not_probe_or_analyze_failed_descriptor_registrations() => _forwardAdapters.Take(2).All(adapter => adapter.ProbeCount == 0 && adapter.AnalyzeCount == 0).ShouldBeTrue();
    [Fact] void should_not_allow_the_synthetic_failure_identity_to_suppress_a_real_adapter() => _forwardValid.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_admit_the_unrelated_valid_adapter() => _forward.Adapters.Single(record => record.Descriptor.Identity.Id == "descriptor-failure").Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_keep_multiple_descriptor_failures_deterministic_under_reversed_input() => Projection(_reverse).ShouldEqual(Projection(_forward));
    [Fact] void should_keep_the_reversed_unrelated_adapter_valid() => _reverseValid.AnalyzeCount.ShouldEqual(1);

    static (ModernAdapter[] Adapters, ModernAdapter Valid) Adapters()
    {
        var first = new ModernAdapter(Descriptor("unreachable-first"))
        {
            OnDescriptor = () => throw new InvalidOperationException("secret /checkout/first")
        };
        var second = new ModernAdapter(Descriptor("unreachable-second"))
        {
            OnDescriptor = () => throw new ArgumentException("secret /private/second")
        };
        var valid = new ModernAdapter(Descriptor("descriptor-failure"));
        return ([first, second, valid], valid);
    }

    static AdapterRunRecord[] Failed(AdapterRunSnapshot snapshot) =>
        [.. snapshot.Adapters.Where(record => record.Descriptor.Identity.Id == "runner:descriptor-failure")];

    static string Projection(AdapterRunSnapshot snapshot) => string.Join(
        '|',
        snapshot.Adapters.Select(record =>
            $"{record.Descriptor.Identity.Id}:{record.Disposition}:{string.Join(',', record.Execution.Diagnostics.Select(diagnostic => diagnostic.Message))}"));
}
