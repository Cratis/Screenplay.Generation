// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_lowering_a_complete_specification_scenario : given.specification_facts
{
    ScreenplayLoweringResult _result = null!;

    void Because()
    {
        var graph = new GenerationResolver().Resolve(
        [
            Contribution(
                FirstAdapter,
                CommandArtifact(),
                EventArtifact(),
                CommandPlacement(),
                EventPlacement(),
                Scenario(StepKey(0), StepKey(1)),
                Step(0, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey(), [ValueKey(0, "name")]),
                Step(1, SpecificationStepPhase.Then, SpecificationStepKind.Event, EventKey(), [ValueKey(1, "name")]),
                Value(0, "name", "Cratis"),
                Value(1, "name", "Cratis"))
        ]);
        _result = new ScreenplayLowerer().Lower(graph, "Banking");
    }

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_attach_the_specification_to_the_exact_slice() => Specification().Name.ShouldEqual("RegisteringAccount");
    [Fact] void should_lower_the_when_command() => Specification().When.CommandType.ShouldEqual("RegisterAccount");
    [Fact] void should_lower_the_then_event() => Specification().ThenEvents.Single().EventType.ShouldEqual("AccountOpened");
    [Fact] void should_lower_exact_command_values() => Specification().When.Values.Single().Property.ShouldEqual("name");
    [Fact] void should_preserve_the_specification_source_file() => Specification().File.Path.ShouldEqual("Accounts/RegisteringAccount.cs");

    SpecificationSyntax Specification() =>
        _result.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
