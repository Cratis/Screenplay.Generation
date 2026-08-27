// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Describes source-neutral evidence considered while probing adapter applicability.
/// </summary>
public sealed record AdapterProbeEvidence
{
    /// <summary>
    /// Gets a human-readable explanation of what the probe observed.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the source range that established the observation, when available.
    /// </summary>
    public SourceRange? Source { get; init; }

    /// <summary>
    /// Gets the source-level subject associated with the observation, when available.
    /// </summary>
    public SubjectId? Subject { get; init; }
}

/// <summary>
/// Represents the structured result of probing one adapter.
/// </summary>
public abstract record AdapterProbeResult
{
    /// <summary>
    /// Gets the canonical evidence supporting the result.
    /// </summary>
    public ImmutableArray<AdapterProbeEvidence> Evidence { get; init; } = [];
}

/// <summary>
/// Indicates that an adapter found no applicable source evidence.
/// </summary>
public sealed record AdapterProbeNotApplicable : AdapterProbeResult;

/// <summary>
/// Indicates that an adapter found sufficient source evidence to execute.
/// </summary>
public sealed record AdapterProbeApplicable : AdapterProbeResult;

/// <summary>
/// Indicates that source evidence applies but safe adapter execution is blocked.
/// </summary>
public sealed record AdapterProbeBlocked : AdapterProbeResult
{
    /// <summary>
    /// Gets diagnostics explaining why execution is blocked.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
