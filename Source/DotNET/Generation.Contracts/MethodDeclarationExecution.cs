// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Observes execution of a declaration for input transfer without asserting an artifact or relationship.</summary>
public sealed record MethodDeclarationExecutionDefinition
{
    /// <summary>Gets the exact executable subject that owns this observation.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets the exact reusable declaration executed by the method.</summary>
    public required SubjectId Declaration { get; init; }

    /// <summary>Gets the source backend occurrence; null selects only the unscoped occurrence.</summary>
    public BackendScopeKey? SourceScope { get; init; }

    /// <summary>Gets the declaration backend occurrence; null selects only the unscoped occurrence.</summary>
    public BackendScopeKey? TargetScope { get; init; }

    /// <summary>Gets the proved nonnegative semantic position, or null when unknown.</summary>
    /// <remarks>Generation never substitutes fact identity or enumeration order for execution precedence.</remarks>
    public int? ExecutionOrder { get; init; }
}

/// <summary>Contributes input-only execution evidence; the subject must equal the exact method.</summary>
/// <remarks>This observation does not classify the declaration as an artifact or create a relationship.</remarks>
public sealed record MethodDeclarationExecutionFact : GenerationFact
{
    /// <summary>Gets the executed declaration occurrence and optional semantic position.</summary>
    public required MethodDeclarationExecutionDefinition Definition { get; init; }
}
