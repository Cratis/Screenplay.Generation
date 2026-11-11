// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterProbeAdmission;

public class when_required_capabilities_are_not_proven : given.a_probe_admission_context
{
    AdapterRunRecord _blocked = null!;
    AdapterRunRecord _satisfied = null!;
    AdapterRunRecord _blockedProbe = null!;
    ProbingAdapter _blockedAdapter = null!;

    void Because()
    {
        (_blocked, _blockedAdapter) = Run(
            Applicable(Evidence("proves mid", "mid.cap"), Evidence("unrelated", "other.cap")),
            null,
            "zeta.cap",
            "alpha.cap",
            "mid.cap");
        _satisfied = Probe(Applicable(Evidence("proves mid", "mid.cap")), null, "mid.cap");
        _blockedProbe = Probe(
            new AdapterProbeBlocked
            {
                Evidence = [Evidence("kept", "kept.cap")],
                Diagnostics = [new GenerationDiagnostic { Code = "PROBE", Severity = GenerationDiagnosticSeverity.Error, Message = "blocked by adapter" }]
            },
            null,
            "never.checked");
    }

    [Fact] void should_block_the_applicable_probe() => _blocked.Disposition.ShouldEqual(AdapterRunDisposition.Blocked);
    [Fact] void should_keep_the_evidence_of_the_blocked_probe_in_canonical_order() => string.Join(';', _blocked.Probe.Evidence.Select(item => item.Description)).ShouldEqual("proves mid;unrelated");
    [Fact]
    void should_report_the_missing_capabilities_in_the_admitted_descriptor_order() => Diagnostics(_blocked.Probe).ShouldEqual(
        Describe(DotNetAdapterGenerationDiagnosticCodes.MissingApiCapabilityEvidence, "Adapter 'probe-adapter' applicable probe did not prove required API capability 'alpha.cap'") + ";" +
        Describe(DotNetAdapterGenerationDiagnosticCodes.MissingApiCapabilityEvidence, "Adapter 'probe-adapter' applicable probe did not prove required API capability 'zeta.cap'"));
    [Fact] void should_not_analyze_the_blocked_probe() => _blockedAdapter.AnalyzeCount.ShouldEqual(0);
    [Fact] void should_admit_an_applicable_probe_that_proves_every_capability() => _satisfied.Probe.ShouldBeOfExactType<AdapterProbeApplicable>();
    [Fact] void should_keep_a_blocked_probe_with_its_diagnostics() => Diagnostics(_blockedProbe.Probe).ShouldEqual("PROBE|blocked by adapter");
    [Fact] void should_keep_the_evidence_of_a_blocked_probe() => _blockedProbe.Probe.Evidence.Single().Description.ShouldEqual("kept");
}
