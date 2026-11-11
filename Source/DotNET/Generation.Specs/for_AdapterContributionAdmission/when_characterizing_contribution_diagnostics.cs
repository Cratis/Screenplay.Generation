// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.for_AdapterContributionAdmission;

public class when_characterizing_contribution_diagnostics : given.a_characterized_admission
{
    string[] _observed = null!;

    void Because()
    {
        var artifactId = Id("artifact");
        var cases = new List<(string, AdapterContributionAdmissionResult)>
        {
            ("valid", AdmitDiagnostic(Diagnostic())),
            ("blank-code-and-message", AdmitDiagnostic(Diagnostic() with { Code = " ", Message = "" })),
            ("unknown-severity", AdmitDiagnostic(Diagnostic() with { Severity = GenerationDiagnosticSeverity.Unknown })),
            ("undefined-severity", AdmitDiagnostic(Diagnostic() with { Severity = (GenerationDiagnosticSeverity)99 })),
            ("undefined-outcome", AdmitDiagnostic(Diagnostic() with { Outcome = (GenerationDiagnosticOutcome)99 })),
            ("defined-outcome", AdmitDiagnostic(Diagnostic() with { Outcome = GenerationDiagnosticOutcome.Unsupported })),
            ("known-fact", AdmitDiagnostic(Diagnostic() with { Facts = [artifactId] })),
            ("unknown-fact", AdmitDiagnostic(Diagnostic() with { Facts = [Id("missing")] })),
            ("unscoped-fact", AdmitDiagnostic(Diagnostic() with { Facts = [new FactId { Value = "other:artifact" }] })),
            ("prefix-only-fact", AdmitDiagnostic(Diagnostic() with { Facts = [new FactId { Value = "atomic:" }] })),
            ("blank-fact", AdmitDiagnostic(Diagnostic() with { Facts = [new FactId { Value = " " }] })),
            ("duplicate-known-fact", AdmitDiagnostic(Diagnostic() with { Facts = [artifactId, artifactId] })),
            ("duplicate-unknown-fact", AdmitDiagnostic(Diagnostic() with { Facts = [Id("missing"), Id("missing")] })),
            ("invalid-subject", AdmitDiagnostic(Diagnostic() with { Subject = new SubjectId { Value = "bad subject" } })),
            ("valid-subject", AdmitDiagnostic(Diagnostic() with { Subject = ArtifactSubject })),
            ("second-diagnostic-invalid", Admit(contribution: Contribution(diagnostics: [Diagnostic(), Diagnostic() with { Code = "" }]))),
            ("everything-wrong", AdmitDiagnostic(Diagnostic() with
            {
                Code = "",
                Message = "",
                Severity = GenerationDiagnosticSeverity.Unknown,
                Outcome = (GenerationDiagnosticOutcome)42,
                Facts = [new FactId { Value = "x" }, new FactId { Value = "x" }],
                Subject = new SubjectId { Value = "no" },
                Source = Source() with { StartLine = 0 }
            }))
        };
        _observed = RenderCompact(cases);
    }

    [Fact] void should_report_the_diagnostic_validation_in_order() => string.Join('\n', _observed).ShouldEqual(Expected);

    const string Expected = """
valid => admitted
blank-code-and-message => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Code
blank-code-and-message => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Message
unknown-severity => UnknownEnumValue|Contribution.Diagnostics[0].Severity
undefined-severity => UndefinedEnumValue|Contribution.Diagnostics[0].Severity
undefined-outcome => UndefinedEnumValue|Contribution.Diagnostics[0].Outcome
defined-outcome => admitted
known-fact => admitted
unknown-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
unscoped-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
prefix-only-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
blank-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
duplicate-known-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[1]
duplicate-unknown-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
duplicate-unknown-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[1]
duplicate-unknown-fact => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[1]
invalid-subject => InvalidSubject|Contribution.Diagnostics[0].Subject
valid-subject => admitted
second-diagnostic-invalid => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Code
everything-wrong => UnknownEnumValue|Contribution.Diagnostics[0].Severity
everything-wrong => UndefinedEnumValue|Contribution.Diagnostics[0].Outcome
everything-wrong => InvalidSubject|Contribution.Diagnostics[0].Subject
everything-wrong => InvalidSourceRange|Contribution.Diagnostics[0].Source
everything-wrong => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Code
everything-wrong => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[0]
everything-wrong => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[1]
everything-wrong => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Facts[1]
everything-wrong => InvalidContributionDiagnostic|Contribution.Diagnostics[0].Message
""";

    static GenerationDiagnostic Diagnostic() => new()
    {
        Code = "ATOMIC0001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Message = "Something",
        Facts = ImmutableArray<FactId>.Empty
    };

    static AdapterContributionAdmissionResult AdmitDiagnostic(GenerationDiagnostic diagnostic) =>
        Admit(contribution: Contribution(diagnostics: [diagnostic]));
}
