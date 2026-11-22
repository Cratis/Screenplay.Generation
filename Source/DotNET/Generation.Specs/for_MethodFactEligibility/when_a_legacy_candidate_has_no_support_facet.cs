// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_a_legacy_candidate_has_no_support_facet : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Classification(MethodExecutionKind.FailureOnly)]);

    [Fact] void should_preserve_legacy_behavior() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_not_publish_a_warning_for_an_unrelated_classification() => _result.Diagnostics.ShouldBeEmpty();
}
