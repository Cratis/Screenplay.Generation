// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_lowering_an_optional_query_specification_without_a_when : given.specification_facts
{
    ResolvedApplicationGraph _graph = null!;
    ScreenplayLoweringResult _lowering = null!;
    string _source = null!;
    string _reversedSource = null!;

    void Because()
    {
        var contribution = Contribution(
                FirstAdapter,
                QueryArtifact(),
                ReadModelArtifact(),
                QueryPlacement(),
                ReadModelPlacement(),
                QueryReturnsReadModel(),
                ScenarioFor(QueryKey(), StepKey(0)),
                Step(
                    0,
                    SpecificationStepPhase.Then,
                    SpecificationStepKind.Read,
                    QueryKey(),
                    [ValueKey(0, "arguments", "accountId"), ValueKey(0, "result", "0", "name")]),
                ValueAt(0, ["arguments", "accountId"], "account-1"),
                ValueAt(0, ["result", "0", "name"], "Screenplay"));
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
    [Fact] void should_lower_the_query() => Specification().ThenQueries.Single().Query.ShouldEqual("AccountById");
    [Fact] void should_lower_the_exact_argument() => Specification().ThenQueries.Single().Arguments.Single().Property.ShouldEqual("accountId");
    [Fact] void should_lower_one_exact_result() => Specification().ThenQueries.Single().Results.Single().Properties.Single().Property.ShouldEqual("name");
    [Fact] void should_be_independent_of_fact_order() => _reversedSource.ShouldEqual(_source);

    Cratis.Screenplay.Syntax.Specifications.SpecificationSyntax Specification() =>
        _lowering.Application.Modules.SelectMany(_ => _.Features).SelectMany(_ => _.Slices).Single(_ => _.Specifications.Any()).Specifications.Single();
}
