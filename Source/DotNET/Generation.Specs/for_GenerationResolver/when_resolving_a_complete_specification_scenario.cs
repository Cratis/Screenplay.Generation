// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_a_complete_specification_scenario : given.specification_facts
{
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reversed = null!;

    void Because()
    {
        GenerationFact[] facts =
        [
            CommandArtifact(),
            EventArtifact(),
            CommandPlacement(),
            Scenario(StepKey(0), StepKey(1)),
            Step(0, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey(), [ValueKey(0, "name")]),
            Step(1, SpecificationStepPhase.Then, SpecificationStepKind.Event, EventKey(), [ValueKey(1, "name")]),
            Value(0, "name", "Cratis"),
            Value(1, "name", "Cratis")
        ];
        var reversedFacts = facts.ToArray();
        Array.Reverse(reversedFacts);
        var resolver = new GenerationResolver();
        _forward = resolver.Resolve([Contribution(FirstAdapter, facts)]);
        _reversed = resolver.Resolve([Contribution(FirstAdapter, reversedFacts)]);
    }

    [Fact] void should_have_no_diagnostics() => _forward.Diagnostics.ShouldBeEmpty();
    [Fact] void should_resolve_the_scenario_fact() => _forward.SpecificationScenarios.Count.ShouldEqual(1);
    [Fact] void should_resolve_every_step_fact() => _forward.SpecificationSteps.Count.ShouldEqual(2);
    [Fact] void should_resolve_every_value_fact() => _forward.SpecificationValues.Count.ShouldEqual(2);
    [Fact] void should_admit_the_complete_scenario() => _forward.Specifications.Count.ShouldEqual(1);
    [Fact] void should_attach_the_exact_target_placement() => _forward.Specifications.Single().Placement.Slice.ShouldEqual("Register");
    [Fact] void should_preserve_authored_step_order() => _forward.Specifications.Single().Steps.Select(step => step.Definition.Key.Index).SequenceEqual([0, 1]).ShouldBeTrue();
    [Fact] void should_preserve_step_level_evidence() => _forward.Specifications.Single().Steps.All(step => step.Evidence.Count == 1).ShouldBeTrue();
    [Fact] void should_be_deterministic_under_fact_permutation() => Snapshot(_reversed).ShouldEqual(Snapshot(_forward));
    [Fact] void should_order_diagnostics_identically_under_fact_permutation() => _reversed.Diagnostics.ShouldContainOnly(_forward.Diagnostics);

    static string Snapshot(ResolvedApplicationGraph graph)
    {
        var scenario = graph.Specifications.Single();
        var steps = scenario.Steps.Select(step => $"{step.Definition.Key.Index}:{step.Definition.Phase}:{step.Definition.Kind}:" +
            string.Join(',', step.Values.Select(value => $"{string.Join('.', value.Definition.Key.Path)}={value.Definition.Scalar}")));
        return $"{scenario.Definition.Key.Scenario.Value}:{scenario.Definition.Name}:{scenario.Placement.Module}:" +
            $"{scenario.Placement.Slice}:{string.Join('|', steps)}";
    }
}
