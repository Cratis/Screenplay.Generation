// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Generation.for_GenerationResolver;

public class when_resolving_diagnostics_from_distinct_source_files_with_same_display_path : given.facts
{
    ResolvedApplicationGraph _forward = null!;
    ResolvedApplicationGraph _reverse = null!;

    void Because()
    {
        var banking = Contribution(FirstAdapter, Diagnostic("Banking"));
        var shipping = Contribution(FirstAdapter, Diagnostic("Shipping"));
        _forward = new GenerationResolver().Resolve([banking, shipping]);
        _reverse = new GenerationResolver().Resolve([shipping, banking]);
    }

    [Fact] void should_retain_both_project_qualified_diagnostics() => _forward.Diagnostics.Count.ShouldEqual(2);
    [Fact] void should_order_diagnostics_independently_of_contribution_order() => JsonSerializer.Serialize(_reverse.Diagnostics).ShouldEqual(JsonSerializer.Serialize(_forward.Diagnostics));
    [Fact] void should_preserve_each_stable_source_identity() => _forward.Diagnostics.Select(_ => _.Source.FileIdentity.Project).ShouldContainOnly("Banking", "Shipping");

    static AdapterContribution Contribution(AdapterIdentity adapter, GenerationDiagnostic diagnostic) => new()
    {
        Adapter = adapter,
        Diagnostics = [diagnostic]
    };

    static GenerationDiagnostic Diagnostic(string project) => new()
    {
        Code = "ADAPTER0001",
        Severity = GenerationDiagnosticSeverity.Warning,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = "Shared diagnostic",
        Source = new SourceRange
        {
            Path = "Common/Order.cs",
            FileIdentity = new SourceFileIdentity { Project = project, Path = "Common/Order.cs" },
            StartLine = 1,
            StartColumn = 1,
            EndLine = 1,
            EndColumn = 10
        },
        Subject = EventSubject
    };
}
