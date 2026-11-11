// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Represents the immutable source-structure snapshot established by one .NET analysis context.
/// </summary>
public sealed record DotNetSourceStructureSnapshot
{
    /// <summary>
    /// Gets source structures in stable project and subject order.
    /// </summary>
    public IReadOnlyList<DotNetSourceStructure> Structures { get; init; } = [];

    /// <summary>
    /// Gets diagnostics that prevented a complete source-structure snapshot.
    /// </summary>
    public IReadOnlyList<GenerationDiagnostic> Diagnostics { get; init; } = [];

    /// <summary>
    /// Gets whether every authored type has stable source-structure evidence.
    /// </summary>
    public bool IsSuccess => Diagnostics.Count == 0;
}

/// <summary>
/// Creates deterministic fixed source-structure snapshots from host-mapped .NET projects.
/// </summary>
public static class DotNetSourceStructures
{
    /// <summary>
    /// Creates one source-structure snapshot from the supplied analysis context.
    /// </summary>
    /// <param name="context">The fixed .NET analysis context.</param>
    /// <returns>The canonical source structures and typed diagnostics.</returns>
    public static DotNetSourceStructureSnapshot Create(DotNetAnalysisContext context)
    {
        var structures = new List<DotNetSourceStructure>();
        var diagnostics = new List<GenerationDiagnostic>();

        foreach (var project in context.Projects)
        {
            if (project.Role is not DotNetProjectRole.Application and not DotNetProjectRole.Specifications)
            {
                diagnostics.Add(Unsupported(
                    DotNetSourceStructureDiagnosticCodes.UnsupportedProjectRole,
                    $"Project '{project.Name}' has unsupported role '{project.Role}'"));
                continue;
            }

            if (project.SourceContext is not { } sourceContext)
            {
                diagnostics.Add(Unsupported(
                    DotNetSourceStructureDiagnosticCodes.MissingSourceContext,
                    $"Project '{project.Name}' has no host-supplied source context"));
                continue;
            }

            AddStructures(project, sourceContext, structures, diagnostics);
        }

        var duplicateSubjects = structures
            .GroupBy(_ => _.Subject)
            .Where(_ => _.Count() > 1)
            .OrderBy(_ => _.Key.Value, StringComparer.Ordinal)
            .ToArray();
        foreach (var duplicate in duplicateSubjects)
        {
            diagnostics.Add(new GenerationDiagnostic
            {
                Code = DotNetSourceStructureDiagnosticCodes.DuplicateSourceSubject,
                Severity = GenerationDiagnosticSeverity.Error,
                Outcome = GenerationDiagnosticOutcome.Conflict,
                Message = $"Source subject '{duplicate.Key.Value}' is declared by more than one analyzed project",
                Subject = duplicate.Key
            });
        }

        var duplicates = duplicateSubjects.Select(_ => _.Key).ToHashSet();
        return new()
        {
            Structures =
            [
                .. structures
                    .Where(_ => !duplicates.Contains(_.Subject))
                    .OrderBy(_ => _.Project, StringComparer.Ordinal)
                    .ThenBy(_ => _.Subject.Value, StringComparer.Ordinal)
            ],
            Diagnostics =
            [
                .. diagnostics
                    .OrderBy(_ => _.Code, StringComparer.Ordinal)
                    .ThenBy(_ => _.Subject?.Value, StringComparer.Ordinal)
                    .ThenBy(_ => _.Message, StringComparer.Ordinal)
            ]
        };
    }

    static void AddStructures(
        DotNetProjectCompilation project,
        DotNetProjectSourceContext sourceContext,
        List<DotNetSourceStructure> structures,
        List<GenerationDiagnostic> diagnostics)
    {
        foreach (var type in new DotNetArtifactCatalog(project.Compilation).Types)
        {
            var declarations = DotNetSource.AuthoredDeclarationsOf(type, project.AuthoredSyntaxTrees);
            if (declarations.Count == 0)
            {
                continue;
            }

            var subject = project.SubjectForType(type);
            var mappedDeclarations = MapDeclarations(declarations, sourceContext);
            if (mappedDeclarations is null)
            {
                diagnostics.Add(Unsupported(
                    DotNetSourceStructureDiagnosticCodes.MissingSourceMapping,
                    $"An authored declaration for subject '{subject.Value}' has no host-supplied source mapping") with
                {
                    Subject = subject
                });
                continue;
            }

            structures.Add(StructureOf(project, sourceContext, type, subject, mappedDeclarations));
        }
    }

    static List<MappedDeclaration>? MapDeclarations(
        IReadOnlyList<SyntaxReference> declarations,
        DotNetProjectSourceContext sourceContext)
    {
        var mappedDeclarations = new List<MappedDeclaration>();
        foreach (var declaration in declarations)
        {
            if (!sourceContext.Files.TryGetValue(declaration.SyntaxTree, out var sourceFile))
            {
                return null;
            }

            mappedDeclarations.Add(new(
                sourceFile,
                declaration.GetSyntax().GetLocation(),
                declaration.Span.Start));
        }

        return mappedDeclarations;
    }

    static DotNetSourceStructure StructureOf(
        DotNetProjectCompilation project,
        DotNetProjectSourceContext sourceContext,
        INamedTypeSymbol type,
        SubjectId subject,
        List<MappedDeclaration> mappedDeclarations)
    {
        var orderedDeclarations = mappedDeclarations
            .OrderBy(_ => _.SourceFile.ProjectRelativePath, StringComparer.Ordinal)
            .ThenBy(_ => _.SpanStart)
            .ToArray();
        return new DotNetSourceStructure
        {
            Subject = subject,
            Project = sourceContext.ProjectIdentity,
            ProjectRole = project.Role,
            Namespace = type.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : type.ContainingNamespace.ToDisplayString(),
            ProjectRelativePaths =
            [
                .. orderedDeclarations
                    .Select(_ => _.SourceFile.ProjectRelativePath)
                    .Distinct(StringComparer.Ordinal)
            ],
            Source = DotNetSource.RangeForProject(orderedDeclarations[0].Location, project)
        };
    }

    static GenerationDiagnostic Unsupported(string code, string message) => new()
    {
        Code = code,
        Severity = GenerationDiagnosticSeverity.Error,
        Outcome = GenerationDiagnosticOutcome.Unsupported,
        Message = message
    };

    sealed record MappedDeclaration(DotNetSourceFile SourceFile, Location Location, int SpanStart);
}
