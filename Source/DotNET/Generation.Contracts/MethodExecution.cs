// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Identifies the execution stage of an exact executable.</summary>
public enum MethodExecutionKind
{
    /// <summary>The execution stage is unknown.</summary>
    Unknown = -1,

    /// <summary>The executable participates in ordinary behavior.</summary>
    Ordinary = 0,

    /// <summary>The executable runs only on failure.</summary>
    FailureOnly = 1
}

/// <summary>Describes one exact executable's execution stage.</summary>
public sealed record MethodExecutionClassificationDefinition
{
    /// <summary>Gets the exact executable identity, also the fact subject.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets its exact backend occurrence; null means unscoped.</summary>
    public BackendScopeKey? Scope { get; init; }

    /// <summary>Gets the execution classification.</summary>
    public required MethodExecutionKind Kind { get; init; }

    /// <summary>Gets the producer's original located warning for failure-only behavior.</summary>
    public GenerationDiagnostic? OmissionReason { get; init; }
}

/// <summary>Classifies an exact executable independently of semantic fact producers.</summary>
public sealed record MethodExecutionClassificationFact : GenerationFact
{
    /// <summary>Gets the exact execution classification.</summary>
    public required MethodExecutionClassificationDefinition Definition { get; init; }
}
