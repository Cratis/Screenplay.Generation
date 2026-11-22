// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_another_adapter_classifies_with_a_foreign_fact_id : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Classification(MethodExecutionKind.FailureOnly) with { Id = Usage().Id }]);

    [Fact] void should_keep_the_other_adapters_fact() => _result.Facts.Length.ShouldEqual(1);
}
