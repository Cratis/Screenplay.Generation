// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Screenplay.Generation.for_ScreenplayDefinitionGenerator;

public class when_generating_from_adapter_run_snapshots_in_different_orders : given.a_generator
{
    const string ExpectedContributionHash = "A063FD97809E3FA5B3A0539E12077C6F04B73F2292705510CA457FF7BFD7228E";
    readonly AdapterIdentity _firstAdapter = new() { Id = "adapter:first", Version = "1.0.0" };
    readonly AdapterIdentity _secondAdapter = new() { Id = "adapter:second", Version = "1.0.0" };
    GeneratedScreenplayDefinition _contribution = null!;
    GeneratedScreenplayDefinition _forward = null!;
    GeneratedScreenplayDefinition _reverse = null!;

    void Because()
    {
        var opened = ProducedBy(Event("AccountOpened", "Open"), _firstAdapter);
        var deposited = ProducedBy(Event("FundsDeposited", "Deposit", Property("amount", "Decimal")), _secondAdapter);
        var firstDiagnostics = Diagnostics("FIRST", opened[0].Subject, _firstAdapter);
        var secondDiagnostics = Diagnostics("SECOND", deposited[0].Subject, _secondAdapter);
        var options = new ScreenplayGenerationOptions { Domain = "Banking" };
        _contribution = Generator.Generate(
            [
                ContributionFrom(_secondAdapter, deposited, secondDiagnostics),
                ContributionFrom(_firstAdapter, opened, firstDiagnostics)
            ],
            options);
        _forward = Generator.Generate(
            Snapshot(
                Completed(_secondAdapter, deposited, secondDiagnostics),
                Completed(_firstAdapter, opened, firstDiagnostics)),
            options);
        _reverse = Generator.Generate(
            Snapshot(
                Completed(
                    _firstAdapter,
                    [.. opened.AsEnumerable().Reverse()],
                    [.. firstDiagnostics.AsEnumerable().Reverse()]),
                Completed(
                    _secondAdapter,
                    [.. deposited.AsEnumerable().Reverse()],
                    [.. secondDiagnostics.AsEnumerable().Reverse()])),
            options);
    }

    [Fact] void should_preserve_the_existing_contribution_source_hash() => Hash(_contribution.Source).ShouldEqual(ExpectedContributionHash);
    [Fact] void should_leave_the_contribution_result_without_an_adapter_run() => _contribution.AdapterRun.ShouldBeNull();
    [Fact] void should_generate_the_same_source_as_the_contribution_overload() => _forward.Source.ShouldEqual(_contribution.Source);
    [Fact] void should_preserve_the_contribution_diagnostics() => _forward.Diagnostics.ShouldContainOnly(_contribution.Diagnostics);
    [Fact] void should_generate_identical_source_after_reversing_adapters_and_facts() => _reverse.Source.ShouldEqual(_forward.Source);
    [Fact] void should_generate_identical_diagnostics_after_reversing_adapters_and_facts() => _reverse.Diagnostics.ShouldContainOnly(_forward.Diagnostics);
    [Fact] void should_return_recursively_identical_canonical_adapter_runs() => AdapterRunProjection(_reverse.AdapterRun).ShouldEqual(AdapterRunProjection(_forward.AdapterRun));
    [Fact] void should_classify_every_admitted_fact_as_lowered() => _forward.AdapterRun!.Facts.All(record => record.Disposition == GenerationFactDisposition.Lowered).ShouldBeTrue();
    [Fact] void should_have_no_unknown_fact_dispositions() => _forward.AdapterRun!.Facts.Any(record => record.Disposition == GenerationFactDisposition.Unknown).ShouldBeFalse();

    static GenerationFact[] ProducedBy(IEnumerable<GenerationFact> facts, AdapterIdentity adapter) =>
        [.. facts.Select(fact => fact with { Evidence = fact.Evidence with { Adapter = adapter } })];

    static AdapterContribution ContributionFrom(
        AdapterIdentity adapter,
        IReadOnlyList<GenerationFact> facts,
        IReadOnlyList<GenerationDiagnostic> diagnostics) => new()
    {
        Adapter = adapter,
        Facts = facts,
        Diagnostics = diagnostics
    };

    static GenerationDiagnostic[] Diagnostics(string code, SubjectId subject, AdapterIdentity adapter) =>
    [
        new GenerationDiagnostic
        {
            Code = code,
            Severity = GenerationDiagnosticSeverity.Information,
            Message = $"Produced by {adapter.Id}",
            Source = new SourceRange
            {
                Path = $"Diagnostics/{adapter.Id}.cs",
                FileIdentity = new SourceFileIdentity
                {
                    Project = "Banking",
                    Path = $"Diagnostics/{adapter.Id}.cs"
                },
                StartLine = 2,
                StartColumn = 3,
                EndLine = 4,
                EndColumn = 5
            },
            Subject = subject
        }
    ];

    static string Hash(string source) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
}
