// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines options controlling one Screenplay generation.
/// </summary>
public sealed record ScreenplayGenerationOptions
{
    /// <summary>
    /// Gets the generated document's domain name.
    /// </summary>
    public required string Domain { get; init; }
}

/// <summary>
/// Represents a generated and verified Screenplay document.
/// </summary>
public sealed record GeneratedScreenplayDefinition
{
    /// <summary>
    /// Gets the canonical generated source.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the generated syntax tree.
    /// </summary>
    public required ApplicationSyntax Application { get; init; }

    /// <summary>
    /// Gets the resolved semantic graph the document was lowered from.
    /// </summary>
    public required ResolvedApplicationGraph Graph { get; init; }

    /// <summary>
    /// Gets all adapter, resolution, lowering, and verification diagnostics.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets the immutable adapter run with final fact dispositions when generation started from a run snapshot.
    /// </summary>
    public AdapterRunSnapshot? AdapterRun { get; init; }

    /// <summary>
    /// Gets whether generation completed without errors.
    /// </summary>
    public bool IsSuccess => Diagnostics.All(_ => _.Severity != GenerationDiagnosticSeverity.Error);
}

/// <summary>
/// Generates and verifies Screenplay documents from framework-neutral adapter contributions.
/// </summary>
/// <param name="resolver">The semantic fact resolver.</param>
/// <param name="lowerer">The Screenplay syntax lowerer.</param>
/// <param name="printer">The canonical Screenplay printer.</param>
/// <param name="compiler">The canonical Screenplay compiler used to verify output.</param>
public sealed class ScreenplayDefinitionGenerator(
    GenerationResolver resolver,
    ScreenplayLowerer lowerer,
    IScreenplayPrinter printer,
    IScreenplayCompiler compiler)
{
    /// <summary>
    /// Initializes a generator with the canonical resolver, lowerer, printer, and compiler.
    /// </summary>
    public ScreenplayDefinitionGenerator()
        : this(new GenerationResolver(), new ScreenplayLowerer(), new ScreenplayPrinter(), new ScreenplayCompiler())
    {
    }

    /// <summary>
    /// Generates one Screenplay document from all adapter contributions.
    /// </summary>
    /// <param name="contributions">The adapter contributions to merge and lower.</param>
    /// <param name="options">Options controlling the generated document.</param>
    /// <returns>The generated and verified definition.</returns>
    public GeneratedScreenplayDefinition Generate(
        IEnumerable<AdapterContribution> contributions,
        ScreenplayGenerationOptions options)
    {
        var graph = resolver.Resolve(contributions);
        var lowering = lowerer.Lower(graph, options.Domain);
        var source = printer.Print(lowering.Application);
        var verification = compiler.Compile(source);
        var verificationDiagnostics = VerificationDiagnostics(verification).ToList();
        if (verification.Success && printer.Print(verification.Value!) != source)
        {
            verificationDiagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnstableRoundTrip,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = "The generated Screenplay document changed after compile and canonical reprint"
            });
        }

        var diagnostics = graph.Diagnostics
            .Concat(lowering.Diagnostics)
            .Concat(verificationDiagnostics)
            .OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .ToArray();

        return new()
        {
            Source = source,
            Application = lowering.Application,
            Graph = graph,
            Diagnostics = diagnostics
        };
    }

    /// <summary>
    /// Generates one Screenplay document from an immutable adapter run snapshot.
    /// </summary>
    /// <param name="snapshot">The immutable source-adapter run snapshot.</param>
    /// <param name="options">Options controlling the generated document.</param>
    /// <returns>The generated and verified definition with final fact dispositions.</returns>
    public GeneratedScreenplayDefinition Generate(
        AdapterRunSnapshot snapshot,
        ScreenplayGenerationOptions options)
    {
        var canonicalAdapters = CanonicalAdapters(snapshot.Adapters);
        var completed = canonicalAdapters
            .Select(record => record.Execution)
            .OfType<AdapterExecutionCompleted>()
            .Select(execution => execution.Contribution)
            .OrderBy(contribution => contribution.Descriptor.Identity.Id, StringComparer.Ordinal)
            .ThenBy(contribution => contribution.Descriptor.Identity.Version, StringComparer.Ordinal)
            .ToArray();
        var facts = completed
            .SelectMany(contribution => contribution.Facts.Select(fact => new ProducedFact(contribution.Descriptor.Identity, fact)))
            .OrderBy(item => item.Producer.Id, StringComparer.Ordinal)
            .ThenBy(item => item.Producer.Version, StringComparer.Ordinal)
            .ThenBy(item => item.Fact.Id.Value, StringComparer.Ordinal)
            .ThenBy(item => item.Fact.Subject.Value, StringComparer.Ordinal)
            .ThenBy(item => Structural.FactFamily(item.Fact))
            .ThenBy(item => Structural.FactDefinition(item.Fact), StringComparer.Ordinal)
            .ThenBy(item => Structural.Evidence(item.Fact.Evidence), StringComparer.Ordinal)
            .Select(item => item.Fact)
            .ToArray();
        var derivation = GenerationFactDerivation.Derive(new AdapterRunSnapshot
        {
            Facts = [.. facts.Select(fact => new GenerationFactRecord { Fact = fact })]
        });
        var contributions = completed.Select(contribution => new AdapterContribution
        {
            Adapter = contribution.Descriptor.Identity,
            Facts = contribution.Facts,
            Diagnostics = contribution.Diagnostics
        });
        var graph = resolver.Resolve(contributions);
        var lowering = lowerer.Lower(graph, options.Domain);
        var source = printer.Print(lowering.Application);
        var verification = compiler.Compile(source);
        var verificationDiagnostics = VerificationDiagnostics(verification).ToList();
        if (verification.Success && printer.Print(verification.Value!) != source)
        {
            verificationDiagnostics.Add(new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.UnstableRoundTrip,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = "The generated Screenplay document changed after compile and canonical reprint"
            });
        }

        var pipelineDiagnostics = graph.Diagnostics
            .Concat(derivation.Diagnostics)
            .Concat(lowering.Diagnostics)
            .Concat(verificationDiagnostics)
            .OrderBy(Canonical.Diagnostic, StringComparer.Ordinal)
            .ToArray();
        var factRecords = GenerationFactDispositionCalculator.Calculate(
            facts,
            graph,
            lowering.Coverage,
            pipelineDiagnostics);
        var canonicalFactRecords = AdapterRunCanonicalizer.FactRecords(factRecords);
        var runnerDiagnostics = RunnerDiagnostics(snapshot.Diagnostics, canonicalAdapters);
        var dispositionDiagnostics = canonicalFactRecords.SelectMany(record => record.Diagnostics).ToArray();
        var adapterRun = new AdapterRunSnapshot
        {
            Adapters = canonicalAdapters,
            Facts = canonicalFactRecords,
            Derivation = derivation,
            Diagnostics = CanonicalDiagnostics(runnerDiagnostics.Concat(dispositionDiagnostics))
        };
        var diagnostics = CanonicalDiagnostics(
            pipelineDiagnostics
                .Concat(runnerDiagnostics)
                .Concat(dispositionDiagnostics));

        return new()
        {
            Source = source,
            Application = lowering.Application,
            Graph = graph,
            Diagnostics = diagnostics,
            AdapterRun = adapterRun
        };
    }

    static ImmutableArray<AdapterRunRecord> CanonicalAdapters(IEnumerable<AdapterRunRecord> adapters) =>
    [
        .. adapters
            .Select(AdapterRunCanonicalizer.Adapter)
            .OrderBy(record => record.Descriptor.Identity.Id, StringComparer.Ordinal)
            .ThenBy(record => record.Descriptor.Identity.Version, StringComparer.Ordinal)
            .ThenBy(AdapterRecordKey, StringComparer.Ordinal)
    ];

    static string AdapterRecordKey(AdapterRunRecord record) => Structural.AdapterRecord(record);

    static ImmutableArray<GenerationDiagnostic> RunnerDiagnostics(
        IEnumerable<GenerationDiagnostic> diagnostics,
        IEnumerable<AdapterRunRecord> adapters) =>
        CanonicalDiagnostics(diagnostics.Concat(adapters.SelectMany(DiagnosticsFrom)));

    static IEnumerable<GenerationDiagnostic> DiagnosticsFrom(AdapterRunRecord record)
    {
        if (record.Probe is AdapterProbeBlocked blocked)
        {
            foreach (var diagnostic in blocked.Diagnostics)
            {
                yield return diagnostic;
            }
        }

        foreach (var diagnostic in record.Execution.Diagnostics)
        {
            yield return diagnostic;
        }

        if (record.Execution is AdapterExecutionCompleted completed)
        {
            foreach (var diagnostic in completed.Contribution.Diagnostics)
            {
                yield return diagnostic;
            }
        }
    }

    static ImmutableArray<GenerationDiagnostic> CanonicalDiagnostics(IEnumerable<GenerationDiagnostic> diagnostics) =>
        AdapterRunCanonicalizer.Diagnostics(diagnostics);

    static IEnumerable<GenerationDiagnostic> VerificationDiagnostics(CompilationResult<ApplicationSyntax> result)
    {
        if (result.Success)
        {
            return [];
        }

        var errors = result.Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error).ToArray();
        var first = errors.FirstOrDefault();
        return
        [
            new GenerationDiagnostic
            {
                Code = GenerationDiagnosticCodes.DocumentDidNotCompile,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Unsupported,
                Message = first is null
                    ? "The generated Screenplay document did not compile"
                    : $"The generated Screenplay document did not compile: {first.Code} {first.Message}",
                Source = first is null
                    ? null
                    : new SourceRange
                    {
                        Path = first.Location.Path ?? string.Empty,
                        StartLine = first.Location.Line,
                        StartColumn = first.Location.Column,
                        EndLine = first.Location.Line,
                        EndColumn = first.Location.Column
                    }
            }
        ];
    }

    sealed record ProducedFact(AdapterIdentity Producer, GenerationFact Fact);
}
