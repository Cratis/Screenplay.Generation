// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_an_incomplete_specification_scenario : given.specification_facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(
            FirstAdapter,
            CommandArtifact(),
            EventArtifact(),
            CommandPlacement(),
            Scenario(StepKey(0), StepKey(1)),
            Step(0, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey(), [ValueKey(0, "name")]),
            Step(1, SpecificationStepPhase.Then, SpecificationStepKind.Event, EventKey(), [ValueKey(1, "name")]),
            Value(0, "name", "Cratis"))
    ]);

    [Fact] void should_resolve_available_raw_facts_for_provenance() => _result.SpecificationSteps.Count.ShouldEqual(2);
    [Fact] void should_admit_no_partial_scenario() => _result.Specifications.ShouldBeEmpty();
    [Fact] void should_report_the_incomplete_scenario() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.IncompleteSpecificationScenario);
    [Fact] void should_retain_the_scenario_source() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.IncompleteSpecificationScenario).Source.Path.ShouldEqual("Accounts/RegisteringAccount.cs");
}
