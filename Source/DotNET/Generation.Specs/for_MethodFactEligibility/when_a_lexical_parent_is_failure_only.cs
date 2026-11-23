// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_a_lexical_parent_is_failure_only : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(Alternative(enclosing: [Parent])), Classification(MethodExecutionKind.Ordinary), Classification(MethodExecutionKind.FailureOnly, Parent, "parent")]);

    [Fact] void should_omit_nested_observations_without_guessing_names() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_preserve_the_parent_warning() => Projection(_result.Diagnostics.Single()).ShouldEqual(Projection(Reason()));
}
