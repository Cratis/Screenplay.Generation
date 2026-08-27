// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Defines a described .NET Screenplay adapter with structured applicability probing.
/// </summary>
public interface IDescribedDotNetScreenplayAdapter
{
    /// <summary>
    /// Gets the source-neutral adapter descriptor.
    /// </summary>
    AdapterDescriptor Descriptor { get; }

    /// <summary>
    /// Probes the analysis context for exact applicability and API capability evidence.
    /// </summary>
    /// <param name="context">The .NET analysis context.</param>
    /// <returns>The structured probe result.</returns>
    AdapterProbeResult Probe(DotNetAnalysisContext context);

    /// <summary>
    /// Analyzes source and returns a raw contribution for atomic host admission.
    /// </summary>
    /// <param name="context">The .NET analysis context.</param>
    /// <param name="options">Options controlling artifact placement.</param>
    /// <returns>The raw adapter contribution.</returns>
    AdapterContribution Analyze(DotNetAnalysisContext context, DotNetAdapterOptions options);
}
