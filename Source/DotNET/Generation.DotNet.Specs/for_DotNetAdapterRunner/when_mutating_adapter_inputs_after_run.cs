// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_mutating_adapter_inputs_after_run : given.a_runner_context
{
    ModernAdapter _adapter = null!;
    AdapterDescriptor _descriptor = null!;
    AdapterProbeEvidence _evidence = null!;
    List<GenerationFact> _facts = null!;
    GenerationFact _fact = null!;
    AdapterRunSnapshot _snapshot = null!;

    void Establish()
    {
        _descriptor = Descriptor("mutable", factCapabilities: [GenerationFactCapability.Artifact]);
        _evidence = new AdapterProbeEvidence { Description = "Exact framework API evidence" };
        _facts = [];
        var contribution = ArtifactContribution(_descriptor.Identity, facts: _facts);
        _fact = contribution.Facts.Single();
        _adapter = new(_descriptor)
        {
            ProbeResult = new AdapterProbeApplicable { Evidence = [_evidence] },
            Contribution = contribution
        };
    }

    void Because()
    {
        _snapshot = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(_adapter)],
            new DotNetAnalysisContext([]),
            Options);
        _facts.Clear();
    }

    [Fact] void should_keep_the_admitted_fact() => _snapshot.Facts.Length.ShouldEqual(1);
    [Fact] void should_not_retain_the_adapter_fact_object() => ReferenceEquals(_fact, _snapshot.Facts.Single().Fact).ShouldBeFalse();
    [Fact] void should_not_retain_the_adapter_descriptor_object() => ReferenceEquals(_descriptor, _snapshot.Adapters.Single().Descriptor).ShouldBeFalse();
    [Fact] void should_not_retain_the_adapter_probe_evidence_object() => ReferenceEquals(_evidence, _snapshot.Adapters.Single().Probe.Evidence.Single()).ShouldBeFalse();
}
