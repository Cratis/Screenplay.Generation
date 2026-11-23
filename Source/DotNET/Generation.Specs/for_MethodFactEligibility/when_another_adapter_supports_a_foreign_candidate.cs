// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_another_adapter_supports_a_foreign_candidate : given.method_facts
{
    GenerationDerivationSnapshot _result;

    void Because() => _result = Derive([.. CompleteFacts(), Support() with { Evidence = Classification(MethodExecutionKind.Ordinary).Evidence }]);

    [Fact] void should_keep_the_other_adapters_candidate() => _result.Facts.Length.ShouldEqual(1);
    [Fact] void should_report_the_foreign_support_as_invalid() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidMethodFactEligibility).ShouldBeTrue();
}
