// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        var diagnostics = graph.Diagnostics
            .Concat(lowering.Diagnostics)
            .Concat(VerificationDiagnostics(verification))
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
}
