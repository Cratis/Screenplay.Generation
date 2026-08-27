// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission.when_admitting_malformed_contributions;

public class with_a_mismatched_contribution_adapter : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit(contribution: Contribution(adapter: new AdapterIdentity { Id = "other", Version = "1.2.3" }));

    [Fact] void should_reject_the_whole_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_the_typed_adapter_mismatch() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(AdapterContributionAdmissionDiagnosticCode.ContributionAdapterMismatch);
}
