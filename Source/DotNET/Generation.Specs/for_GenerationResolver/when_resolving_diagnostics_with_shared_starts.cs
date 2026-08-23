// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_diagnostics_with_shared_starts : given.facts
{
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var first = Diagnostic(8, GenerationDiagnosticOutcome.Unsupported);
        var second = Diagnostic(12, GenerationDiagnosticOutcome.Unknown);
        _forward = new GenerationResolver().Resolve(
        [
            ContributionWithDiagnostic(FirstAdapter, first),
            ContributionWithDiagnostic(SecondAdapter, second)
        ]);
        _reverse = new GenerationResolver().Resolve(
        [
            ContributionWithDiagnostic(SecondAdapter, second),
            ContributionWithDiagnostic(FirstAdapter, first)
        ]);
    }

    [Fact] void should_order_diagnostics_independently_of_contribution_order() =>
        JsonSerializer.Serialize(_reverse.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_forward.Diagnostics));

    [Fact] void should_use_the_complete_source_range_as_a_tie_breaker() =>
        _forward.Diagnostics.Select(_ => _.Source!.EndLine).ShouldContainOnly(8, 12);

    static AdapterContribution ContributionWithDiagnostic(
        AdapterIdentity adapter,
        GenerationDiagnostic diagnostic) => new()
    {
        Adapter = adapter,
        Diagnostics = [diagnostic]
    };

    static GenerationDiagnostic Diagnostic(int endLine, GenerationDiagnosticOutcome outcome) => new()
    {
        Code = "ADAPTER0001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = outcome,
        Message = "Shared diagnostic",
        Source = new SourceRange
        {
            Path = "Shared.cs",
            StartLine = 1,
            StartColumn = 1,
            EndLine = endLine,
            EndColumn = 1
        },
        Subject = EventSubject
    };
}
