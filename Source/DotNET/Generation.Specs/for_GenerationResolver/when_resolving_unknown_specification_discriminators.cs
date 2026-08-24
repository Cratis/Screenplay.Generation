// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_unknown_specification_discriminators : given.specification_facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(
    [
        Contribution(
            FirstAdapter,
            Step(0, SpecificationStepPhase.Unknown, SpecificationStepKind.Command, CommandKey()),
            Step(1, SpecificationStepPhase.Then, (SpecificationStepKind)99, EventKey()),
            Value(0, "name", "Cratis", SpecificationValueKind.Unknown))
    ]);

    [Fact] void should_omit_the_invalid_steps() => _result.SpecificationSteps.ShouldBeEmpty();
    [Fact] void should_omit_the_invalid_value() => _result.SpecificationValues.ShouldBeEmpty();
    [Fact] void should_report_the_unknown_phase() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedSpecificationStepPhase);
    [Fact] void should_report_the_undefined_step_kind() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedSpecificationStepKind);
    [Fact] void should_report_the_unknown_value_kind() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(GenerationDiagnosticCodes.UnsupportedSpecificationValueKind);
    [Fact] void should_distinguish_unknown_from_unsupported() => _result.Diagnostics.Count(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Unknown).ShouldEqual(2);
    [Fact] void should_report_one_unsupported_discriminator() => _result.Diagnostics.Count(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Unsupported).ShouldEqual(1);
}
