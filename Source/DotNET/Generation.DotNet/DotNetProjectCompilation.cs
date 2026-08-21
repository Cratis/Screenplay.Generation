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
    /// Gets the project file path when the project came from an MSBuild workspace.
    /// </summary>
    public string? ProjectPath { get; init; }

    /// <summary>
    /// Gets the repository or workspace root used to create relative source paths.
    /// </summary>
    public string? SourceRoot { get; init; }

    /// <summary>
    /// Gets the Roslyn compilation.
    /// </summary>
    public required Compilation Compilation { get; init; }
}

/// <summary>
/// Provides deterministic access to all project compilations analyzed together.
/// </summary>
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
}

/// <summary>
/// The exception that is thrown when a syntax tree does not belong to the current .NET analysis context.
/// </summary>
/// <param name="path">The path of the unknown syntax tree.</param>
public sealed class SyntaxTreeNotInAnalysis(string path)
    : Exception($"The syntax tree '{path}' is not part of the .NET analysis context");
