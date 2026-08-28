// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Identifies one deterministic derivation rule and its semantic version.
/// </summary>
public sealed record GenerationDerivationRuleIdentity
{
    /// <summary>
    /// Gets the stable source-neutral rule identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the semantic rule version.
    /// </summary>
    public required string Version { get; init; }
}

/// <summary>
/// Describes the producer, canonical inputs, and complete evidence lineage of one derived fact.
/// </summary>
public sealed record GenerationFactLineage
{
    /// <summary>
    /// Gets the rule that produced the fact.
    /// </summary>
    public required GenerationDerivationRuleIdentity Producer { get; init; }

    /// <summary>
    /// Gets the canonical identities of every admitted base fact used to derive the fact.
    /// </summary>
    public ImmutableArray<FactId> Inputs { get; init; } = [];

    /// <summary>
    /// Gets the canonical evidence from every admitted base fact used to derive the fact.
    /// </summary>
    public ImmutableArray<Evidence> Evidence { get; init; } = [];
}

/// <summary>
/// Records one rule execution over a fixed admitted base snapshot.
/// </summary>
public sealed record GenerationDerivationRuleRecord
{
    /// <summary>
    /// Gets the executed rule identity and version.
    /// </summary>
    public required GenerationDerivationRuleIdentity Rule { get; init; }

    /// <summary>
    /// Gets every admitted base fact considered by the rule in canonical order.
    /// </summary>
    public ImmutableArray<FactId> Inputs { get; init; } = [];

    /// <summary>
    /// Gets every derived output fact identity in canonical order.
    /// </summary>
    public ImmutableArray<FactId> Outputs { get; init; } = [];

    /// <summary>
    /// Gets deterministic diagnostics produced by the rule.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>
/// Represents one immutable derivation pass over a fixed admitted base snapshot.
/// </summary>
public sealed record GenerationDerivationSnapshot
{
    /// <summary>
    /// Gets rule execution records in canonical rule order.
    /// </summary>
    public ImmutableArray<GenerationDerivationRuleRecord> Rules { get; init; } = [];

    /// <summary>
    /// Gets derived fact records in canonical fact order.
    /// </summary>
    public ImmutableArray<GenerationFactRecord> Facts { get; init; } = [];

    /// <summary>
    /// Gets all derivation diagnostics in canonical order.
    /// </summary>
    public ImmutableArray<GenerationDiagnostic> Diagnostics { get; init; } = [];
}
