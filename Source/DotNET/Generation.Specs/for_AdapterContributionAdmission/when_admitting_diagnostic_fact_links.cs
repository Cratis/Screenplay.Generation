// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_admitting_diagnostic_fact_links : given.a_contribution
{
    GenerationDiagnostic _diagnostic = null!;
    AdapterContributionAdmissionResult _result = null!;

    void Establish() => _diagnostic = new GenerationDiagnostic
    {
        Code = "ATOMIC1001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = "The artifact behavior is not represented",
        Facts = [Id("artifact")],
        Subject = ArtifactSubject
    };

    void Because() => _result = Admit(contribution: Contribution(diagnostics: [_diagnostic]));

    [Fact] void should_admit_the_contribution() => _result.IsAdmitted.ShouldBeTrue();
    [Fact] void should_retain_the_exact_fact_link() => _result.Snapshot!.Diagnostics.Single().Facts.Single().ShouldEqual(Id("artifact"));
    [Fact] void should_deep_copy_the_fact_link() => ReferenceEquals(_result.Snapshot!.Diagnostics.Single().Facts.Single(), _diagnostic.Facts.Single()).ShouldBeFalse();
}
