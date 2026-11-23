// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_declaration_input_owner_is_contradictory : given.declaration_input_members
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Surface() with { Subject = ScenarioSubject });

    [Fact] void should_reject_the_cross_owner_surface() => _result.IsAdmitted.ShouldBeFalse();
}
