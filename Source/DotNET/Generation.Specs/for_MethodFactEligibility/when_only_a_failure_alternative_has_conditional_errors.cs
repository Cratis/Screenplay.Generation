// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_only_a_failure_alternative_has_conditional_errors : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(Alternative(diagnostics: [Reason() with { Code = "CONDITIONAL", Severity = GenerationDiagnosticSeverity.Error }]), Alternative(Parent)), Classification(MethodExecutionKind.FailureOnly), Classification(MethodExecutionKind.Ordinary, Parent, "parent")]);

    [Fact] void should_keep_the_independent_ordinary_behavior() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_not_publish_an_inactive_error() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "CONDITIONAL").ShouldBeFalse();
}
