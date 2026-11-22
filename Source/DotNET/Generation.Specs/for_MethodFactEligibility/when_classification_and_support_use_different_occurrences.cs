// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_classification_and_support_use_different_occurrences : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(), Classification(MethodExecutionKind.FailureOnly, scope: new BackendScopeKey { Store = Parent, EventModelName = "other" })]);

    [Fact] void should_not_apply_another_occurrences_failure_classification() => _result.Facts.Length.ShouldEqual(1);
}
