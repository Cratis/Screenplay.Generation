// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_registering_invalid_api_capabilities : given.a_runner_context
{
    ModernAdapter _adapter = null!;
    AdapterRunRecord _record = null!;

    void Establish() => _adapter = new(Descriptor(
        "invalid-api",
        apiCapabilities:
        [
            new AdapterApiCapability { Id = "framework.api" },
            new AdapterApiCapability { Id = " malformed " },
            new AdapterApiCapability { Id = "framework.api" }
        ]));

    void Because() => _record = DotNetAdapterRunner.Run(
        [DotNetAdapterRegistration.For(_adapter)],
        new DotNetAnalysisContext([]),
        Options).Adapters.Single();

    [Fact] void should_reject_the_roster_record_before_probe() => _record.Disposition.ShouldEqual(AdapterRunDisposition.RosterRejected);
    [Fact] void should_not_probe_or_execute_the_adapter() => (_adapter.ProbeCount + _adapter.AnalyzeCount).ShouldEqual(0);
}
