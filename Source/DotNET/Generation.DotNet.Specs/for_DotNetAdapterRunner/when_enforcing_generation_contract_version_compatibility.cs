// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_enforcing_generation_contract_version_compatibility : given.a_runner_context
{
    ModernAdapter _belowMinimum = null!;
    ModernAdapter _atMinimum = null!;
    ModernAdapter _belowMaximum = null!;
    ModernAdapter _atMaximum = null!;
    ModernAdapter _unbounded = null!;
    AdapterRunRecord _belowMinimumRecord = null!;
    AdapterRunRecord _atMinimumRecord = null!;
    AdapterRunRecord _belowMaximumRecord = null!;
    AdapterRunRecord _atMaximumRecord = null!;
    AdapterRunRecord _unboundedRecord = null!;
    AdapterRunRecord _defaultVersionRecord = null!;

    void Establish()
    {
        var bounded = new GenerationVersionRange
        {
            MinimumInclusive = new Version(1, 0, 0),
            MaximumExclusive = new Version(2, 0, 0)
        };
        _belowMinimum = Adapter("below-minimum", bounded);
        _atMinimum = Adapter("at-minimum", bounded);
        _belowMaximum = Adapter("below-maximum", bounded);
        _atMaximum = Adapter("at-maximum", bounded);
        _unbounded = Adapter(
            "unbounded",
            new GenerationVersionRange { MinimumInclusive = new Version(2, 0, 0) });
    }

    void Because()
    {
        _belowMinimumRecord = Run(_belowMinimum, new Version(0, 99, 0));
        _atMinimumRecord = Run(_atMinimum, new Version(1, 0, 0));
        _belowMaximumRecord = Run(_belowMaximum, new Version(1, 99, 0));
        _atMaximumRecord = Run(_atMaximum, new Version(2, 0, 0));
        _unboundedRecord = Run(_unbounded, new Version(99, 0, 0));

        var currentVersion = typeof(AdapterDescriptor).Assembly.GetName().Version!;
        var defaultVersion = Adapter(
            "default-version",
            new GenerationVersionRange { MinimumInclusive = currentVersion });
        _defaultVersionRecord = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(defaultVersion)],
            new DotNetAnalysisContext([]),
            Options).Adapters.Single();
    }

    [Fact] void should_block_a_version_below_the_inclusive_minimum_before_probe() => _belowMinimumRecord.Disposition.ShouldEqual(AdapterRunDisposition.Blocked);
    [Fact] void should_admit_the_inclusive_minimum() => _atMinimumRecord.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_admit_a_version_below_the_exclusive_maximum() => _belowMaximumRecord.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_block_the_exclusive_maximum_before_probe() => _atMaximumRecord.Disposition.ShouldEqual(AdapterRunDisposition.Blocked);
    [Fact] void should_admit_versions_above_an_unbounded_minimum() => _unboundedRecord.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_derive_the_default_from_the_loaded_generation_contracts_assembly() => _defaultVersionRecord.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
    [Fact] void should_not_probe_incompatible_adapters() => (_belowMinimum.ProbeCount + _atMaximum.ProbeCount).ShouldEqual(0);
    [Fact] void should_probe_each_compatible_adapter_once() => new[] { _atMinimum, _belowMaximum, _unbounded }.All(adapter => adapter.ProbeCount == 1).ShouldBeTrue();
    [Fact] void should_report_a_stable_version_diagnostic_without_machine_paths() => IncompatibleDiagnostics().ShouldEqual("DOTNETADAPTER009:Adapter 'at-maximum' supports Generation.Contracts versions from '1.0.0' inclusive through '2.0.0' exclusive, but the runner host version is '2.0.0'");

    string IncompatibleDiagnostics() => string.Join(
        '|',
        _atMaximumRecord.Execution.Diagnostics.Select(diagnostic => $"{diagnostic.Code}:{diagnostic.Message}"));

    static ModernAdapter Adapter(string id, GenerationVersionRange range) =>
        new(Descriptor(id, generationVersions: range));

    static AdapterRunRecord Run(ModernAdapter adapter, Version version) =>
        DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext([]),
            Options,
            version).Adapters.Single();
}
