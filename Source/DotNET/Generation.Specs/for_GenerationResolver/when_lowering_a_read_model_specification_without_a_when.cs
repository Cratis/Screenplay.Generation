// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_lowering_a_read_model_specification_without_a_when : given.specification_facts
{
    ResolvedApplicationGraph _graph = null!;
    ScreenplayLoweringResult _lowering = null!;
    string _source = null!;
    string _reversedSource = null!;

    void Because()
    {
        var contribution = Contribution(
                FirstAdapter,
                EventArtifact(),
                ReadModelArtifact(),
                EventPlacement("Open"),
                ReadModelPlacement(),
                ScenarioFor(ReadModelKey(), StepKey(0), StepKey(1)),
                Step(0, SpecificationStepPhase.Given, SpecificationStepKind.Event, EventKey(), [ValueKey(0, "accountId")]),
                Step(1, SpecificationStepPhase.Then, SpecificationStepKind.ReadModel, ReadModelKey(), [ValueKey(1, "name")]),
                Value(0, "accountId", "account-1"),
                Value(1, "name", "Screenplay"));
        _graph = new GenerationResolver().Resolve([contribution]);
        _lowering = new ScreenplayLowerer().Lower(_graph, "Banking");
        _source = new ScreenplayPrinter().Print(_lowering.Application);
        var reversed = new GenerationResolver().Resolve([contribution with { Facts = [.. contribution.Facts.Reverse()] }]);
        _reversedSource = new ScreenplayPrinter().Print(new ScreenplayLowerer().Lower(reversed, "Banking").Application);
    }

    [Fact] void should_resolve_one_complete_scenario() => _graph.Specifications.Count.ShouldEqual(1);
    [Fact] void should_report_no_resolution_diagnostics() => _graph.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_without_diagnostics() => _lowering.Diagnostics.ShouldBeEmpty();
    [Fact] void should_lower_no_when_step() => Specification().When.ShouldBeNull();
    [Fact] void should_lower_the_given_event() => Specification().Given.Single().EventType.ShouldEqual("AccountOpened");
    [Fact] void should_lower_the_then_read_model() => Specification().ThenReadModels.Single().Name.ShouldEqual("AccountOverview");
    [Fact] void should_lower_the_exact_read_model_value() => Specification().ThenReadModels.Single().Properties.Single().Property.ShouldEqual("name");
    [Fact] void should_be_independent_of_fact_order() => _reversedSource.ShouldEqual(_source);

    Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax Specification() =>
        _lowering.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Single(_ => _.Specifications.Any()).Specifications.Single();
}
