// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_invalid_diagnostic_fact_links : given.a_contribution
{
    AdapterContributionAdmissionResult _result = null!;

    void Because() => _result = Admit(contribution: Contribution(diagnostics:
    [
        new GenerationDiagnostic
        {
            Code = "ATOMIC1002",
            Severity = GenerationDiagnosticSeverity.Error,
            Outcome = GenerationDiagnosticOutcome.Unknown,
            Message = "Malformed typed links",
            Facts =
            [
                Id("missing"),
                new FactId { Value = "foreign:artifact" },
                Id("artifact"),
                Id("artifact"),
                new FactId { Value = $"{Adapter.Id}:" }
            ]
        }
    ]));

    [Fact] void should_reject_the_complete_contribution() => _result.Snapshot.ShouldBeNull();
    [Fact] void should_report_missing_foreign_and_duplicate_links() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == AdapterContributionAdmissionDiagnosticCode.InvalidContributionDiagnostic).ShouldBeGreaterThan(2);
    [Fact] void should_report_an_empty_local_fact_suffix_as_invalid_scope() => _result.Diagnostics.Single(diagnostic => diagnostic.Path.EndsWith(".Facts[4]", StringComparison.Ordinal)).Message.ShouldContain("must be normalized and scoped");
}
