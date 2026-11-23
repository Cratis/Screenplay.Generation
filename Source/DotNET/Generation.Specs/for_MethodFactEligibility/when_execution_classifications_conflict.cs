// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_execution_classifications_conflict : given.method_facts
{
    GenerationDerivationSnapshot _result;
    GenerationDerivationSnapshot _permuted;

    void Because()
    {
        GenerationFact[] facts = [.. CompleteFacts(), Support(), Classification(MethodExecutionKind.FailureOnly), Classification(MethodExecutionKind.Ordinary, id: "ordinary")];
        _result = Derive(facts);
        _permuted = Derive([.. facts.Reverse()]);
    }

    [Fact] void should_fail_closed_before_derivation() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_a_typed_conflict() => _result.Diagnostics.Any(diagnostic => diagnostic.Outcome == GenerationDiagnosticOutcome.Conflict).ShouldBeTrue();
    [Fact] void should_be_independent_of_adapter_order() => Projection(_result).ShouldEqual(Projection(_permuted));
}
