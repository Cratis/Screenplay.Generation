// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_running_a_source_independent_adapter_without_projects : given.a_runner_context
{
    ModernAdapter _adapter = null!;
    AdapterRunRecord _record = null!;

    void Establish() => _adapter = new(Descriptor("source-independent"));

    void Because() => _record = DotNetAdapterRunner.Run(
        [DotNetAdapterRegistration.For(_adapter)],
        new DotNetAnalysisContext([]),
        Options).Adapters.Single();

    [Fact] void should_probe_once() => _adapter.ProbeCount.ShouldEqual(1);
    [Fact] void should_execute_once() => _adapter.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_admit_the_empty_source_neutral_contribution() => _record.Disposition.ShouldEqual(AdapterRunDisposition.Admitted);
}
