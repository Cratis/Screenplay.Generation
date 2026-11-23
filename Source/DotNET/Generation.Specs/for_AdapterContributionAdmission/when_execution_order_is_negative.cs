// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_execution_order_is_negative : given.declaration_input_members
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = AdmitEligibility(Usage() with { Definition = Usage().Definition with { ExecutionOrder = -1 } });

    [Fact] void should_reject_the_invalid_semantic_order() => _result.IsAdmitted.ShouldBeFalse();
}
