// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_method_support_references_another_producer : given.method_eligibility
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Usage() with { Evidence = Evidence() with { Adapter = new AdapterIdentity { Id = "other", Version = "1.0.0" } } }, Support());

    [Fact] void should_reject_the_entire_contribution() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_not_expose_a_partial_snapshot() => _result.Snapshot.ShouldBeNull();
}
