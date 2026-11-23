// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_method_eligibility_capability_is_not_declared : given.method_eligibility
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = Admit(descriptor: Descriptor(GenerationFactCapability.MethodArtifactUsage), contribution: Contribution([Usage(), Support()]));

    [Fact] void should_reject_the_undeclared_fact_family() => _result.IsAdmitted.ShouldBeFalse();
}
