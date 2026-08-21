// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Defines a source-based Screenplay adapter over Roslyn project compilations.
/// </summary>
public interface IDotNetScreenplayAdapter
{
    /// <summary>
    /// Gets the adapter identity and version.
    /// </summary>
    AdapterIdentity Identity { get; }

    /// <summary>
    /// Gets whether the adapter recognizes framework evidence in the analysis context.
    /// </summary>
    /// <param name="context">The .NET analysis context.</param>
    /// <returns><see langword="true"/> when the adapter can contribute facts; otherwise, <see langword="false"/>.</returns>
    bool CanAnalyze(DotNetAnalysisContext context);

    /// <summary>
    /// Analyzes source and returns semantic facts and diagnostics.
    /// </summary>
    /// <param name="context">The .NET analysis context.</param>
    /// <param name="options">Options controlling artifact placement.</param>
    /// <returns>The adapter contribution.</returns>
    AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options);
}

/// <summary>
/// Defines options controlling .NET adapter artifact placement.
/// </summary>
public sealed record DotNetAdapterOptions
{
    /// <summary>
    /// Gets an optional module that all discovered artifacts should be placed beneath.
    /// </summary>
    public string? Module { get; init; }

    /// <summary>
    /// Gets the number of leading namespace segments to omit from inferred features.
    /// </summary>
    public int NamespaceSegmentsToSkip { get; init; }
}
