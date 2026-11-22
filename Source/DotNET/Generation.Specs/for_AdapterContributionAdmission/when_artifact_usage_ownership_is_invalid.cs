// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_artifact_usage_ownership_is_invalid : given.artifact_usages
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = Admit(contribution: Contribution([Declaration() with { Subject = ExternalSubject }, Entry() with { Subject = ExternalSubject }, Usage() with { Subject = ExternalSubject }]));

    [Fact] void should_reject_the_contribution() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_report_each_mismatched_owner() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.OwnershipMismatch).ShouldEqual(3);
}
