// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_one_independent_support_is_ordinary : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(Alternative(), Alternative(Parent)), Classification(MethodExecutionKind.FailureOnly), Classification(MethodExecutionKind.Ordinary, Parent, "parent")]);

    [Fact] void should_keep_the_candidate() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_keep_the_original_failure_warning_as_provenance() => _result.Diagnostics.Single().ShouldEqual(Reason());
    [Fact] void should_retain_the_support_facet_in_lineage() => _result.Facts.Single().Lineage.Inputs.Contains(Support().Id).ShouldBeTrue();
    [Fact] void should_retain_the_ordinary_classification_in_lineage() => _result.Facts.Single().Lineage.Inputs.Contains(Classification(MethodExecutionKind.Ordinary, Parent, "parent").Id).ShouldBeTrue();
    [Fact] void should_not_retain_the_excluded_classification_in_output_lineage() => _result.Facts.Single().Lineage.Inputs.Contains(Classification(MethodExecutionKind.FailureOnly).Id).ShouldBeFalse();
}
