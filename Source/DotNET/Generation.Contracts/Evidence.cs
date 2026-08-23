// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines how strongly source evidence establishes a semantic fact.
/// </summary>
public enum EvidenceStrength
{
    /// <summary>
    /// The adapter could not determine a supported evidence strength.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The source directly and unambiguously declares or performs the fact.
    /// </summary>
    Exact = 0,

    /// <summary>
    /// Framework configuration explicitly establishes the fact.
    /// </summary>
    Configured = 1,

    /// <summary>
    /// A documented framework convention establishes the fact.
    /// </summary>
    Conventional = 2,

    /// <summary>
    /// A naming or structural heuristic suggests the fact but cannot prove it.
    /// </summary>
    Heuristic = 3
}

/// <summary>
/// Identifies an authored source file independently from its display path.
/// </summary>
public sealed record SourceFileIdentity
{
    /// <summary>
    /// Gets the stable project identity that owns the source file.
    /// </summary>
    public required string Project { get; init; }

    /// <summary>
    /// Gets the normalized project-relative source path.
    /// </summary>
    public required string Path { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Project}:{Path}";
}

/// <summary>
/// Identifies a range in authored source.
/// </summary>
public sealed record SourceRange
{
    /// <summary>
    /// Gets the host-declared display path using forward slashes.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Gets the stable source-file identity when the host supplied an explicit source-path policy.
    /// </summary>
    public SourceFileIdentity? FileIdentity { get; init; }

    /// <summary>
    /// Gets the 1-based starting line.
    /// </summary>
    public required int StartLine { get; init; }

    /// <summary>
    /// Gets the 1-based starting column.
    /// </summary>
    public required int StartColumn { get; init; }

    /// <summary>
    /// Gets the 1-based ending line.
    /// </summary>
    public required int EndLine { get; init; }

    /// <summary>
    /// Gets the 1-based ending column.
    /// </summary>
    public required int EndColumn { get; init; }
}

/// <summary>
/// Describes why an adapter asserted a fact and where the evidence came from.
/// </summary>
public sealed record Evidence
{
    /// <summary>
    /// Gets the adapter that observed the evidence.
    /// </summary>
    public required AdapterIdentity Adapter { get; init; }

    /// <summary>
    /// Gets the evidence strength.
    /// </summary>
    public required EvidenceStrength Strength { get; init; }

    /// <summary>
    /// Gets the source range, when authored source establishes the fact.
    /// </summary>
    public SourceRange? Source { get; init; }

    /// <summary>
    /// Gets the human-readable explanation of how the fact was established.
    /// </summary>
    public string? Explanation { get; init; }
}
