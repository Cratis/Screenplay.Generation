// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Represents one loaded .NET project compilation and its source-path context.
/// </summary>
public sealed record DotNetProjectCompilation
{
    /// <summary>
    /// Gets the logical project name without a target-framework suffix.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the role this project has in the analyzed application source set.
    /// </summary>
    public DotNetProjectRole Role { get; init; } = DotNetProjectRole.Application;

    /// <summary>
    /// Gets the project file path when the project came from an MSBuild workspace.
    /// </summary>
    public string? ProjectPath { get; init; }

    /// <summary>
    /// Gets the repository or workspace root used to create relative source paths.
    /// </summary>
    public string? SourceRoot { get; init; }

    /// <summary>
    /// Gets the explicit source-file identity and display mapping when supplied by the host.
    /// </summary>
    /// <remarks>
    /// This context is the sole project source-identity authority. The compilation does not infer identity from its
    /// logical name, project path, source root, or Roslyn syntax-tree paths.
    /// </remarks>
    public DotNetProjectSourceContext? SourceContext { get; init; }

    /// <summary>
    /// Gets the Roslyn compilation.
    /// </summary>
    public required Compilation Compilation { get; init; }

    /// <summary>
    /// Gets the syntax trees known by the workspace host to come from authored project documents.
    /// </summary>
    /// <remarks>
    /// Source-generator trees must not be included. A host can establish this set from project documents before
    /// generators update the compilation; generated-file naming and headers are not an authoritative origin signal.
    /// </remarks>
    public required IReadOnlySet<SyntaxTree> AuthoredSyntaxTrees { get; init; }

    /// <summary>
    /// Gets a project-qualified subject identity for a named type.
    /// </summary>
    /// <param name="type">The type to identify.</param>
    /// <returns>The project-qualified subject identity.</returns>
    public SubjectId SubjectForType(INamedTypeSymbol type) => DotNetSubjectIds.ForType(type, Name);
}

/// <summary>
/// Provides deterministic access to all project compilations analyzed together.
/// </summary>
/// <param name="projects"></param>
public sealed class DotNetAnalysisContext(IEnumerable<DotNetProjectCompilation> projects)
{
    /// <summary>
    /// Gets the projects in canonical order.
    /// </summary>
    public IReadOnlyList<DotNetProjectCompilation> Projects { get; } =
    [
        .. projects
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
            .ThenBy(_ => _.Compilation.AssemblyName, StringComparer.Ordinal)
            .ThenBy(_ => _.ProjectPath, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Gets the semantic model for a syntax tree from the project that owns it.
    /// </summary>
    /// <param name="tree">The syntax tree to get a semantic model for.</param>
    /// <returns>The owning project's semantic model.</returns>
    /// <exception cref="SyntaxTreeNotInAnalysis">The syntax tree is not part of the analysis context.</exception>
    public SemanticModel SemanticModelFor(SyntaxTree tree)
    {
        var project = Projects.FirstOrDefault(_ => _.Compilation.SyntaxTrees.Contains(tree)) ??
                      throw new SyntaxTreeNotInAnalysis(tree.FilePath);

        return project.Compilation.GetSemanticModel(tree);
    }

    /// <summary>
    /// Gets the project containing the syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to locate.</param>
    /// <returns>The project containing the tree.</returns>
    /// <exception cref="SyntaxTreeNotInAnalysis">The syntax tree is not part of the analysis context.</exception>
    public DotNetProjectCompilation ProjectFor(SyntaxTree tree)
    {
        return Projects.FirstOrDefault(_ => _.Compilation.SyntaxTrees.Contains(tree)) ??
               throw new SyntaxTreeNotInAnalysis(tree.FilePath);
    }

    /// <summary>
    /// Gets the project-qualified subject for a source-declared named type when exactly one analyzed project owns it.
    /// </summary>
    /// <param name="type">The type reference to resolve.</param>
    /// <returns>The exact source subject, or <see langword="null"/> when the type is external, missing, or ambiguous.</returns>
    public SubjectId? SubjectForType(INamedTypeSymbol type)
    {
        var metadataName = DotNetSubjectIds.MetadataName(type);
        var candidates = Projects
            .Select(project => new { Project = project, Type = project.Compilation.GetTypeByMetadataName(metadataName) })
            .Where(_ => _.Type?.Locations.Any(location => location.IsInSource) == true)
            .Select(_ => _.Project.SubjectForType(_.Type!))
            .Distinct()
            .ToArray();
        return candidates.Length == 1 ? candidates[0] : null;
    }
}

/// <summary>
/// The exception that is thrown when a syntax tree does not belong to the current .NET analysis context.
/// </summary>
/// <param name="path">The path of the unknown syntax tree.</param>
public sealed class SyntaxTreeNotInAnalysis(string path)
    : Exception($"The syntax tree '{path}' is not part of the .NET analysis context");
