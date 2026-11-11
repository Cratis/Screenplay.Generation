// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterProbeAdmission;

public class when_admitting_applicable_and_not_applicable_probes : given.a_probe_admission_context
{
    AdapterProbeEvidence[] _input = null!;
    AdapterRunRecord _applicable = null!;
    AdapterRunRecord _notApplicable = null!;
    AdapterRunRecord _defaultEvidence = null!;
    ProbingAdapter _applicableAdapter = null!;

    void Establish()
    {
        var project = Project();
        _input =
        [
            Evidence("z last", "b.cap", "urn:example:two", RangeIn(project, 3, 5)),
            Evidence("a first", "b.cap", "urn:example:two", RangeIn(project, 1, 5)),
            Evidence("no capability"),
            Evidence("a first", "a.cap", "https://host", RangeIn(project, 1, 2)),
            Evidence("b second", "b.cap", "urn:example:one", RangeIn(project, 1, 5)),
            Evidence("with spaces inside", "b.cap"),
            Evidence("same range other end", "b.cap", "urn:example:one", RangeIn(project, 1, 3))
        ];
        (_applicable, _applicableAdapter) = Run(Applicable(_input), project);
        _notApplicable = Probe(new AdapterProbeNotApplicable { Evidence = [_input[2]] });
        _defaultEvidence = Probe(new AdapterProbeApplicable { Evidence = default });
    }

    [Fact] void should_admit_the_applicable_probe() => _applicable.Probe.ShouldBeOfExactType<AdapterProbeApplicable>();
    [Fact] void should_analyze_the_applicable_probe() => _applicableAdapter.AnalyzeCount.ShouldEqual(1);
    [Fact] void should_order_the_evidence_canonically() => string.Join(';', _applicable.Probe.Evidence.Select(Describe)).ShouldEqual(
        "no capability|||-;" +
        "a first|a.cap|https://host|1-2;" +
        "with spaces inside|b.cap||-;" +
        "same range other end|b.cap|urn:example:one|1-3;" +
        "b second|b.cap|urn:example:one|1-5;" +
        "a first|b.cap|urn:example:two|1-5;" +
        "z last|b.cap|urn:example:two|3-5");
    [Fact] void should_freeze_the_evidence_into_new_instances() => _applicable.Probe.Evidence.Any(item => _input.Any(original => ReferenceEquals(original, item))).ShouldBeFalse();
    [Fact] void should_admit_the_not_applicable_probe_with_its_evidence() => string.Join(';', _notApplicable.Probe.Evidence.Select(Describe)).ShouldEqual("no capability|||-");
    [Fact] void should_keep_the_not_applicable_probe_kind() => _notApplicable.Probe.ShouldBeOfExactType<AdapterProbeNotApplicable>();
    [Fact] void should_not_analyze_the_not_applicable_probe() => _notApplicable.Disposition.ShouldEqual(AdapterRunDisposition.NotApplicable);
    [Fact] void should_treat_default_evidence_as_empty() => _defaultEvidence.Probe.Evidence.IsEmpty.ShouldBeTrue();
}
