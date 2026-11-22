// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Identifies an exact backend store registration and optional case-sensitive event-model qualifier.
/// </summary>
public sealed record BackendScopeKey
{
    /// <summary>Gets the exact project-qualified store registration subject, not its display name.</summary>
    public required SubjectId Store { get; init; }

    /// <summary>Gets the configured event-model qualifier; null means no qualifier, not unknown ownership.</summary>
    public string? EventModelName { get; init; }
}

/// <summary>
/// Describes generator provenance, not executable storage semantics or Screenplay placement.
/// </summary>
public sealed record BackendScopeDefinition
{
    /// <summary>Gets the exact backend ownership identity.</summary>
    public required BackendScopeKey Key { get; init; }

    /// <summary>Gets an optional display name that never participates in scope identity.</summary>
    public string? Name { get; init; }
}

/// <summary>
/// Establishes backend ownership with a real source location; the subject equals the key's store subject.
/// </summary>
public sealed record BackendScopeFact : GenerationFact
{
    /// <summary>Gets the asserted scope declaration.</summary>
    public required BackendScopeDefinition Definition { get; init; }
}
