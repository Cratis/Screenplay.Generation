// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_modern_probes_are_malformed : given.a_runner_context
{
    ModernAdapter[] _adapters = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        var unknown = new ModernAdapter(Descriptor("unknown")) { ProbeResult = new UnknownProbeResult() };
        var malformedEvidence = new ModernAdapter(Descriptor("malformed-evidence"))
        {
            ProbeResult = new AdapterProbeApplicable
            {
                Evidence = [new AdapterProbeEvidence { Description = " malformed " }]
            }
        };
        var malformedEnum = new ModernAdapter(Descriptor("malformed-enum"))
        {
            ProbeResult = new AdapterProbeBlocked
            {
                Diagnostics =
                [
                    new GenerationDiagnostic
                    {
                        Code = "PROBE",
                        Severity = GenerationDiagnosticSeverity.Unknown,
                        Message = "Malformed severity"
                    }
                ]
            }
        };
        var emptyBlock = new ModernAdapter(Descriptor("empty-block"))
        {
            ProbeResult = new AdapterProbeBlocked { Diagnostics = [] }
        };
        _adapters = [unknown, malformedEvidence, malformedEnum, emptyBlock];
    }

    void Because() => _snapshot = DotNetAdapterRunner.Run(
        _adapters.Select(DotNetAdapterRegistration.For),
        new DotNetAnalysisContext([]),
        Options);

    [Fact] void should_probe_each_adapter_once() => _adapters.All(adapter => adapter.ProbeCount == 1).ShouldBeTrue();
    [Fact] void should_not_execute_any_malformed_probe() => _adapters.All(adapter => adapter.AnalyzeCount == 0).ShouldBeTrue();
    [Fact] void should_block_every_malformed_probe() => _snapshot.Adapters.All(record => record.Disposition == AdapterRunDisposition.Blocked).ShouldBeTrue();
    [Fact] void should_replace_malformed_details_with_stable_probe_diagnostics() => _snapshot.Adapters.All(record => ((AdapterProbeBlocked)record.Probe).Diagnostics.Single().Code == DotNetAdapterGenerationDiagnosticCodes.ProbeRejected).ShouldBeTrue();

    sealed record UnknownProbeResult : AdapterProbeResult;
}
