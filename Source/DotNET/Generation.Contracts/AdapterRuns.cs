// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the final disposition of an adapter considered by a run.
/// </summary>
public enum AdapterRunDisposition
{
    /// <summary>
    /// No disposition has been calculated.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The adapter was not applicable.
    /// </summary>
    NotApplicable = 0,

    /// <summary>
    /// The adapter was applicable but blocked before execution.
    /// </summary>
    Blocked = 1,

    /// <summary>
    /// The adapter did not execute.
    /// </summary>
    NotExecuted = 2,

    /// <summary>
    /// Adapter execution failed.
    /// </summary>
    ExecutionFailed = 3,

    /// <summary>
    /// The adapter executed but its contribution was rejected atomically.
    /// </summary>
    ContributionRejected = 4,

    /// <summary>
    /// The adapter contribution was admitted.
    /// </summary>
    Admitted = 5
}

/// <summary>
/// Defines how one admitted fact was handled by later generation stages.
/// </summary>
public enum GenerationFactDisposition
{
    /// <summary>
    /// No disposition has been calculated.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The fact contributed directly to generated Screenplay syntax.
    /// </summary>
    Lowered = 0,

    /// <summary>
    /// The fact was retained as provenance without contributing syntax directly.
    /// </summary>
    ProvenanceOnly = 1,

    /// <summary>
    /// The fact was omitted and a diagnostic explains the omission.
    /// </summary>
    OmittedWithDiagnostic = 2,

    /// <summary>
    /// The fact participated in an unresolved conflict.
    /// </summary>
    Conflicted = 3
}

/// <summary>
/// Represents the immutable result of executing one adapter.
/// </summary>
public abstract record AdapterExecutionResult
{
    /// <summary>
    /// Gets diagnostics produced by the execution boundary.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Indicates that an adapter was not executed.
/// </summary>
public sealed record AdapterExecutionNotRun : AdapterExecutionResult;

/// <summary>
/// Indicates that adapter execution and contribution admission completed successfully.
/// </summary>
public sealed record AdapterExecutionCompleted : AdapterExecutionResult
{
    /// <summary>
    /// Gets the admitted, deeply frozen contribution.
    /// </summary>
    public required AdapterContributionSnapshot Contribution { get; init; }
}

/// <summary>
/// Indicates that adapter execution completed but contribution admission rejected the result.
/// </summary>
public sealed record AdapterExecutionRejected : AdapterExecutionResult
{
    /// <summary>
    /// Gets the deterministic admission diagnostics.
    /// </summary>
    public ImmutableArray<AdapterContributionAdmissionDiagnostic> AdmissionDiagnostics { get; init; } = [];
}

/// <summary>
/// Indicates that adapter execution failed before it produced an admissible contribution.
/// </summary>
public sealed record AdapterExecutionFailed : AdapterExecutionResult;

/// <summary>
/// Represents one deeply frozen and canonically ordered adapter contribution.
/// </summary>
public sealed record AdapterContributionSnapshot
{
    /// <summary>
    /// Gets the canonical descriptor under which the contribution was admitted.
    /// </summary>
    public required AdapterDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the deeply frozen facts in canonical identity order.
    /// </summary>
    public ImmutableArray<GenerationFact> Facts { get; init; } = [];

    /// <summary>
    /// Gets the deeply frozen contribution diagnostics in canonical order.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Records one admitted fact and its later generation disposition.
/// </summary>
public sealed record GenerationFactRecord
{
    /// <summary>
    /// Gets the admitted fact.
    /// </summary>
    public required GenerationFact Fact { get; init; }

    /// <summary>
    /// Gets the disposition calculated by later generation stages.
    /// </summary>
    public GenerationFactDisposition Disposition { get; init; } = GenerationFactDisposition.Unknown;

    /// <summary>
    /// Gets diagnostics specifically associated with the disposition.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Records how one adapter was considered during a run.
/// </summary>
public sealed record AdapterRunRecord
{
    /// <summary>
    /// Gets the adapter descriptor.
    /// </summary>
    public required AdapterDescriptor Descriptor { get; init; }

    /// <summary>
    /// Gets the structured probe result.
    /// </summary>
    public required AdapterProbeResult Probe { get; init; }

    /// <summary>
    /// Gets the execution result, defaulting to not run.
    /// </summary>
    public AdapterExecutionResult Execution { get; init; } = new AdapterExecutionNotRun();

    /// <summary>
    /// Gets the final adapter disposition.
    /// </summary>
    public AdapterRunDisposition Disposition { get; init; } = AdapterRunDisposition.Unknown;
}

/// <summary>
/// Represents one immutable source-adapter run snapshot.
/// </summary>
public sealed record AdapterRunSnapshot
{
    /// <summary>
    /// Gets per-adapter run records in canonical descriptor order.
    /// </summary>
    public ImmutableArray<AdapterRunRecord> Adapters { get; init; } = [];

    /// <summary>
    /// Gets admitted fact records in canonical fact order.
    /// </summary>
    public ImmutableArray<GenerationFactRecord> Facts { get; init; } = [];

    /// <summary>
    /// Gets run-level diagnostics in canonical order.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
