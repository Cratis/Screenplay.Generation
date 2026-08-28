// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_an_undeclared_fact_capability : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit(Descriptor(GenerationFactCapability.Artifact));

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_each_undeclared_runtime_fact_family() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.UndeclaredFactCapability).ShouldEqual(13);
}
