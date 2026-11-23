// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_method_support_has_no_alternatives : given.method_eligibility
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Usage(), Support([]));

    [Fact] void should_reject_instead_of_making_the_candidate_unconditional() => _result.IsAdmitted.ShouldBeFalse();
}
