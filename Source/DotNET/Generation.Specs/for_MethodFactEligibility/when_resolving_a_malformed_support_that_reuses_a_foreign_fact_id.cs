// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_MethodFactEligibility;

public class when_resolving_a_malformed_support_that_reuses_a_foreign_fact_id : given.method_facts
{
    static readonly AdapterIdentity _otherAdapter = new() { Id = "other", Version = "1.0.0" };
    ResolvedApplicationGraph _result = null!;

    void Because()
    {
        var support = Support() with
        {
            Id = Usage().Id,
            Evidence = UsageEvidence(_otherAdapter),
            Definition = Support().Definition with { Alternatives = [null!] }
        };
        _result = new GenerationResolver().Resolve([.. Contributions(), new AdapterContribution { Adapter = _otherAdapter, Facts = [support] }]);
    }

    [Fact] void should_report_the_conflicting_fact_identity() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.ConflictingFactIdentity && diagnostic.Facts.Contains(Usage().Id)).ShouldBeTrue();
    [Fact] void should_report_the_invalid_support() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == GenerationDiagnosticCodes.InvalidMethodFactEligibility).ShouldBeTrue();
}
