// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_running_duplicate_adapter_ids : given.a_runner_context
{
    ModernAdapter _first = null!;
    ModernAdapter _second = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        _first = new(Descriptor("duplicate", "2.0.0"));
        _second = new(Descriptor("duplicate", "1.0.0"));
    }

    void Because() => _snapshot = DotNetAdapterRunner.Run(
        [DotNetAdapterRegistration.For(_first), DotNetAdapterRegistration.For(_second)],
        new DotNetAnalysisContext([]),
        Options);

    [Fact] void should_not_probe_any_duplicate() => (_first.ProbeCount + _second.ProbeCount).ShouldEqual(0);
    [Fact] void should_not_execute_any_duplicate() => (_first.AnalyzeCount + _second.AnalyzeCount).ShouldEqual(0);
    [Fact] void should_reject_every_duplicate_registration() => _snapshot.Adapters.All(record => record.Considered && !record.Probed && !record.Executed && record.Disposition == AdapterRunDisposition.RosterRejected).ShouldBeTrue();
    [Fact] void should_order_duplicate_versions_ordinally() => _snapshot.Adapters.Select(record => record.Descriptor.Identity.Version).ShouldEqual(["1.0.0", "2.0.0"]);
}
