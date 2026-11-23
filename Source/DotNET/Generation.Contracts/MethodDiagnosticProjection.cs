// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>Specifies the explicitly requested attribution of a method diagnostic.</summary>
public enum MethodDiagnosticProjectionMode
{
    /// <summary>No supported projection was selected.</summary>
    Unknown = 0,

    /// <summary>Attribute the payload to one exact independently observed entry-point artifact.</summary>
    EntryPointArtifact = 1
}

/// <summary>Requests diagnostic attribution without changing the original invocation evidence.</summary>
public sealed record MethodDiagnosticProjectionDefinition
{
    /// <summary>Gets the exact opaque method identity.</summary>
    public required SubjectId Method { get; init; }

    /// <summary>Gets exact backend ownership; null means unscoped, not a wildcard.</summary>
    public BackendScopeKey? Scope { get; init; }

    /// <summary>Gets the requested Command or Query entry-point role.</summary>
    public required ArtifactKind EntryPointRole { get; init; }

    /// <summary>Gets the explicit projection mode.</summary>
    public required MethodDiagnosticProjectionMode Mode { get; init; }

    /// <summary>Gets the located adapter payload with nonempty same-contribution supporting fact references.</summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}

/// <summary>An opt-in method diagnostic intent, subject to the existing method eligibility contract.</summary>
public sealed record MethodDiagnosticProjectionFact : GenerationFact
{
    /// <summary>Gets the requested diagnostic projection.</summary>
    public required MethodDiagnosticProjectionDefinition Definition { get; init; }
}
