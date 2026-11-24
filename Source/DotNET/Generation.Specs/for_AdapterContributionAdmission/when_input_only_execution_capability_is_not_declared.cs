// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_input_only_execution_capability_is_not_declared : given.method_declaration_execution
{
    AdapterContributionAdmissionResult _result;

    void Because() => _result = Admit(descriptor: Descriptor(GenerationFactCapability.MethodArtifactUsage), contribution: Contribution([Execution()]));

    [Fact] void should_reject_the_undeclared_input_only_fact_family() => _result.IsAdmitted.ShouldBeFalse();
    [Fact] void should_not_treat_artifact_usage_as_the_same_capability() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.UndeclaredFactCapability).ShouldBeTrue();
}
