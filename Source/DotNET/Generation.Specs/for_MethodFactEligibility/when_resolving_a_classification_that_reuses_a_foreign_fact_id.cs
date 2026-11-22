// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_resolving_a_classification_that_reuses_a_foreign_fact_id : given.method_facts
{
    ResolvedApplicationGraph _result = null!;

    void Because() => _result = new GenerationResolver().Resolve(Contributions(Classification(MethodExecutionKind.FailureOnly) with { Id = Usage().Id }));

    [Fact] void should_report_the_conflicting_fact_identity() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingFactIdentity && diagnostic.Facts.Contains(Usage().Id)).ShouldBeTrue();
}
