// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Groups adapter-owned facts that share one fallback omission explanation.
/// </summary>
public sealed record OmittedFactDiagnosticGroupFact : GenerationFact
{
    /// <summary>Gets the explicitly covered facts and their located diagnostic.</summary>
    public required OmittedFactDiagnosticGroupDefinition Definition { get; init; }
}

/// <summary>
/// Describes a diagnostic that replaces only generic omission noise, never specific failures.
/// </summary>
public sealed record OmittedFactDiagnosticGroupDefinition
{
    /// <summary>Gets the nonempty, disjoint set of facts owned by the contributing adapter.</summary>
    public IReadOnlyList<FactId> CoveredFacts { get; init; } = [];

    /// <summary>Gets the located explanation, with the same subject and source as the group fact.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}
