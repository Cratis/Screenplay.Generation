// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterProbeAdmission;

public class when_admitting_unsupported_probe_results : given.a_probe_admission_context
{
    AdapterRunRecord _missing = null!;
    AdapterRunRecord _notRun = null!;
    ProbingAdapter _missingAdapter = null!;

    void Because()
    {
        (_missing, _missingAdapter) = Run(null);
        _notRun = Probe(new AdapterProbeNotRun());
    }

    [Fact] void should_reject_a_missing_probe() => Diagnostics(_missing.Probe).ShouldEqual(Rejected);
    [Fact] void should_reject_a_not_run_probe() => Diagnostics(_notRun.Probe).ShouldEqual(Rejected);
    [Fact] void should_block_both() => (_missing.Disposition, _notRun.Disposition).ShouldEqual((AdapterRunDisposition.Blocked, AdapterRunDisposition.Blocked));
    [Fact] void should_not_analyze_a_rejected_probe() => _missingAdapter.AnalyzeCount.ShouldEqual(0);
}
