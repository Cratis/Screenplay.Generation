// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_only_another_overload_is_failure_only : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(), Classification(MethodExecutionKind.FailureOnly, Overload)]);

    [Fact] void should_join_the_exact_executable_only() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_not_guess_classification_by_display_name() => _result.Diagnostics.ShouldBeEmpty();
}
