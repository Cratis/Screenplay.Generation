// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_a_legacy_classification_has_an_unsupported_kind : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support(), Classification((MethodExecutionKind)37)]);

    [Fact] void should_not_reinterpret_the_classification_as_absent() => _result.Facts.ShouldBeEmpty();
    [Fact] void should_report_unknown_metadata() => _result.Diagnostics.Single().Outcome.ShouldEqual(GenerationDiagnosticOutcome.Unknown);
}
