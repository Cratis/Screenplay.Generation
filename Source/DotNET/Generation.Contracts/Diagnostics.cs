// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Defines the severity of a generation diagnostic.
/// </summary>
public enum GenerationDiagnosticSeverity
{
    /// <summary>
    /// Informational context that does not make the result incomplete.
    /// </summary>
    Information = 0,

    /// <summary>
    /// Recognized behavior was approximated or omitted, but useful output remains.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// The generated result cannot be trusted or compiled.
    /// </summary>
    Error = 2
}

/// <summary>
/// Represents a stable, located diagnostic produced while generating a Screenplay document.
/// </summary>
public sealed record GenerationDiagnostic
{
    /// <summary>
    /// Gets the stable diagnostic code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Gets the diagnostic severity.
    /// </summary>
    public required GenerationDiagnosticSeverity Severity { get; init; }

    /// <summary>
    /// Gets the human-readable diagnostic message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Gets the source range associated with the diagnostic, when available.
    /// </summary>
    public SourceRange? Source { get; init; }

    /// <summary>
    /// Gets the affected subject, when the diagnostic concerns one source-level subject.
    /// </summary>
    public SubjectId? Subject { get; init; }
}
