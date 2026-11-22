// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Describes an independent method observation supporting a semantic candidate.</summary>
public sealed record MethodFactSupportAlternative
{
    /// <summary>Gets the exact observing executable.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets the explicit lexical enclosing executables supplied by the frontend.</summary>
    public IReadOnlyList<SubjectId> EnclosingExecutables { get; init; } = [];

    /// <summary>Gets the exact observing backend occurrence; null means unscoped.</summary>
    public BackendScopeKey? Scope { get; init; }

    /// <summary>Gets semantic diagnostics conditional on this observation.</summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];
}

/// <summary>Describes alternative observations for a same-producer semantic candidate.</summary>
public sealed record MethodFactSupportDefinition
{
    /// <summary>Gets the candidate fact identity from the same contribution.</summary>
    public required FactId Candidate { get; init; }

    /// <summary>Gets independent alternatives; any eligible alternative retains the candidate.</summary>
    public required IReadOnlyList<MethodFactSupportAlternative> Alternatives { get; init; }
}

/// <summary>Conditions one semantic candidate on its producer's exact method observations.</summary>
public sealed record MethodFactSupportFact : GenerationFact
{
    /// <summary>Gets the support definition; this fact has the candidate's subject.</summary>
    public required MethodFactSupportDefinition Definition { get; init; }
}
