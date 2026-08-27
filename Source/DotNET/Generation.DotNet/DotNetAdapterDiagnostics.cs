// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Generation.DotNet;

static class DotNetAdapterDiagnostics
{
    public static GenerationDiagnostic Error(string code, string adapterId, string detail) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = $"Adapter '{adapterId}' {detail}"
    };

    public static GenerationDiagnostic HostError(string code, string detail) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Error,
        Message = $"The .NET adapter host {detail}"
    };

    public static GenerationDiagnostic OperationFailed(string adapterId, string operation, Exception exception) =>
        Error(
            DotNetAdapterGenerationDiagnosticCodes.OperationFailed,
            adapterId,
            $"operation '{operation}' failed with exception type '{exception.GetType().FullName ?? exception.GetType().Name}'");

    public static ImmutableArray<GenerationDiagnostic> Canonical(IEnumerable<GenerationDiagnostic> diagnostics) =>
    [
        .. diagnostics.Select(Freeze)
            .Distinct()
            .OrderBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => (int)diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Outcome is null ? int.MinValue : (int)diagnostic.Outcome.Value)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Project, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.FileIdentity?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.Path, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Source?.StartLine)
            .ThenBy(diagnostic => diagnostic.Source?.StartColumn)
            .ThenBy(diagnostic => diagnostic.Source?.EndLine)
            .ThenBy(diagnostic => diagnostic.Source?.EndColumn)
            .ThenBy(diagnostic => diagnostic.Subject?.Value, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)
    ];

    static GenerationDiagnostic Freeze(GenerationDiagnostic diagnostic) => new()
    {
        Code = diagnostic.Code,
        Severity = diagnostic.Severity,
        Message = diagnostic.Message,
        Outcome = diagnostic.Outcome,
        Source = diagnostic.Source is null ? null : Freeze(diagnostic.Source),
        Subject = diagnostic.Subject is null ? null : new SubjectId { Value = diagnostic.Subject.Value }
    };

    static SourceRange Freeze(SourceRange source) => new()
    {
        Path = source.Path,
        FileIdentity = source.FileIdentity is null
            ? null
            : new SourceFileIdentity
            {
                Project = source.FileIdentity.Project,
                Path = source.FileIdentity.Path
            },
        StartLine = source.StartLine,
        StartColumn = source.StartColumn,
        EndLine = source.EndLine,
        EndColumn = source.EndColumn
    };
}
