// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_required_capabilities_are_missing : given.a_runner_context
{
    ModernAdapter _missingApi = null!;
    ModernAdapter _missingHost = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        _missingApi = new(Descriptor(
            "missing-api",
            apiCapabilities: [new AdapterApiCapability { Id = "framework.api" }]));
        _missingHost = new(Descriptor(
            "missing-host",
            language: AdapterSourceLanguage.CSharp,
            hostCapabilities: [AdapterHostCapability.SemanticAnalysis]));
    }

    void Because() => _snapshot = DotNetAdapterRunner.Run(
        [DotNetAdapterRegistration.For(_missingApi), DotNetAdapterRegistration.For(_missingHost)],
        new DotNetAnalysisContext([]),
        Options);

    [Fact] void should_block_before_probe_for_a_missing_host_capability() => _missingHost.ProbeCount.ShouldEqual(0);
    [Fact] void should_block_after_probe_for_missing_api_capability_evidence() => _missingApi.ProbeCount.ShouldEqual(1);
    [Fact] void should_not_execute_either_adapter() => (_missingApi.AnalyzeCount + _missingHost.AnalyzeCount).ShouldEqual(0);
    [Fact] void should_record_both_as_blocked() => _snapshot.Adapters.All(record => record.Disposition == AdapterRunDisposition.Blocked).ShouldBeTrue();
}
