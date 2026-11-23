// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_failure_classification_has_no_located_warning : given.method_eligibility
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Classification() with { Definition = Classification().Definition with { Kind = MethodExecutionKind.FailureOnly } });

    [Fact] void should_reject_instead_of_inventing_a_warning() => _result.IsAdmitted.ShouldBeFalse();
}
