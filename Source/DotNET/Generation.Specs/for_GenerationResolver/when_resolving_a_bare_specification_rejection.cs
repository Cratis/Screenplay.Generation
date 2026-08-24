// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_a_bare_specification_rejection : given.specification_facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(
            FirstAdapter,
            CommandArtifact(),
            CommandPlacement(),
            Scenario(StepKey(0), StepKey(1)),
            Step(0, SpecificationStepPhase.When, SpecificationStepKind.Command, CommandKey()),
            Step(1, SpecificationStepPhase.Then, SpecificationStepKind.Error, null))
    ]);

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_admit_the_whole_scenario() => _result.Specifications.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_bare_error() => _result.Specifications.Single().Steps.Single(step => step.Definition.Kind == SpecificationStepKind.Error).Definition.ErrorMessage.ShouldBeNull();
}
