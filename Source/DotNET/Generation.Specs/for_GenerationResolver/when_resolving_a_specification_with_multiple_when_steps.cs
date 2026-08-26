// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_a_specification_with_multiple_when_steps : given.specification_facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(
            FirstAdapter,
            CommandArtifact(),
            EventArtifact(),
            CommandPlacement(),
            EventPlacement(),
            Scenario(StepKey(0), StepKey(1), StepKey(2)),
            Step(0, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey()),
            Step(1, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey()),
            Step(2, SpecificationStepPhase.Then, SpecificationStepKind.Event, EventKey()))
    ]);

    [Fact] void should_admit_no_partial_scenario() => _result.Specifications.ShouldBeEmpty();
    [Fact] void should_report_only_the_incomplete_scenario() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly(GenerationDiagnosticCodes.IncompleteSpecificationScenario);
}
