// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Defines which declared relative path is presented in generated output and diagnostics.
/// </summary>
public enum DotNetSourceDisplayRoot
{
    /// <summary>
    /// The host supplied an unknown display-root policy.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Paths are displayed relative to the selected workspace or application root.
    /// </summary>
    Workspace = 0,

    /// <summary>
    /// Paths are displayed relative to each owning project.
    /// </summary>
    Project = 1
}

/// <summary>
/// Defines how stable source identities treat path casing.
/// </summary>
public enum DotNetSourcePathCasePolicy
{
    /// <summary>
    /// The host supplied an unknown case policy.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Path identity uses ordinal case-sensitive comparison.
    /// </summary>
    Ordinal = 0,

    /// <summary>
    /// Path identity is folded using invariant lowercase and then NFC-normalized before ordinal comparison.
    /// </summary>
    InvariantLowercase = 1
}

/// <summary>
/// Describes the explicit host-owned source-path compatibility policy.
/// </summary>
public sealed record DotNetSourcePathPolicy
{
    /// <summary>
    /// Gets the source-path contract version.
    /// </summary>
    public int Version { get; init; } = 1;

    /// <summary>
    /// Gets the display root.
    /// </summary>
    public required DotNetSourceDisplayRoot DisplayRoot { get; init; }

    /// <summary>
    /// Gets the stable identity case policy.
    /// </summary>
    public required DotNetSourcePathCasePolicy CasePolicy { get; init; }
}

/// <summary>
/// Describes one authored source document using host-declared logical paths.
/// </summary>
public sealed record DotNetSourceDocument
{
    /// <summary>
    /// Gets the authored syntax tree. Its Roslyn file path can retain the physical checkout path.
    /// </summary>
    public required SyntaxTree SyntaxTree { get; init; }

    /// <summary>
    /// Gets the source path relative to the owning project.
    /// </summary>
    public required string ProjectRelativePath { get; init; }

    /// <summary>
    /// Gets the source path relative to the selected workspace or application root.
    /// </summary>
    public required string WorkspaceRelativePath { get; init; }
}

/// <summary>
/// Maps one authored syntax tree to stable identity and display paths.
/// </summary>
public sealed record DotNetSourceFile
{
    /// <summary>
    /// Gets the stable source-file identity.
    /// </summary>
    public required SourceFileIdentity Identity { get; init; }

    /// <summary>
    /// Gets the path presented in generated output and diagnostics.
    /// </summary>
    public required string DisplayPath { get; init; }
}

/// <summary>
/// Creates deterministic source-file mappings from host-declared logical paths.
/// </summary>
public static class DotNetSourcePaths
{
    /// <summary>
    /// Creates an immutable project source context.
    /// </summary>
    /// <param name="projectIdentity">The stable project identity.</param>
    /// <param name="policy">The explicit source-path policy.</param>
    /// <param name="documents">The authored source documents.</param>
    /// <returns>The immutable source context.</returns>
    /// <exception cref="InvalidDotNetProjectIdentity">The project identity is malformed.</exception>
    /// <exception cref="InvalidDotNetSourcePath">A declared source path is malformed, rooted, or traversing.</exception>
    /// <exception cref="UnsupportedDotNetSourcePathPolicy">The declared policy version or value is unsupported.</exception>
    /// <exception cref="DuplicateDotNetSourceIdentity">Two documents resolve to the same stable source identity.</exception>
    /// <exception cref="DuplicateDotNetSourceTree">One syntax tree is supplied more than once.</exception>
    public static DotNetProjectSourceContext Create(
        string projectIdentity,
        DotNetSourcePathPolicy policy,
        IEnumerable<DotNetSourceDocument> documents)
    {
        var normalizedProject = NormalizeProjectIdentity(projectIdentity);
        ValidatePolicy(policy);

        var files = new Dictionary<SyntaxTree, DotNetSourceFile>();
        var identities = new HashSet<SourceFileIdentity>();
        foreach (var document in documents
                     .OrderBy(_ => _.ProjectRelativePath, StringComparer.Ordinal)
                     .ThenBy(_ => _.WorkspaceRelativePath, StringComparer.Ordinal))
        {
            var projectPath = NormalizeRelativePath(document.ProjectRelativePath);
            var workspacePath = NormalizeRelativePath(document.WorkspaceRelativePath);
            var identityPath = IdentityPath(projectPath, policy.CasePolicy);
            var identity = new SourceFileIdentity { Project = normalizedProject, Path = identityPath };
            if (!identities.Add(identity))
            {
                throw new DuplicateDotNetSourceIdentity(identity);
            }

            var sourceFile = new DotNetSourceFile
            {
                Identity = identity,
                DisplayPath = policy.DisplayRoot == DotNetSourceDisplayRoot.Workspace ? workspacePath : projectPath
            };
            if (!files.TryAdd(document.SyntaxTree, sourceFile))
            {
                throw new DuplicateDotNetSourceTree(document.SyntaxTree.FilePath);
            }
        }

        return new(normalizedProject, policy, files);
    }

    static void ValidatePolicy(DotNetSourcePathPolicy policy)
    {
        if (policy.Version != 1)
        {
            throw new UnsupportedDotNetSourcePathPolicy($"version '{policy.Version}'");
        }

        if (policy.DisplayRoot is not DotNetSourceDisplayRoot.Workspace and not DotNetSourceDisplayRoot.Project)
        {
            throw new UnsupportedDotNetSourcePathPolicy($"display root '{policy.DisplayRoot}'");
        }

        if (policy.CasePolicy is not DotNetSourcePathCasePolicy.Ordinal and not DotNetSourcePathCasePolicy.InvariantLowercase)
        {
            throw new UnsupportedDotNetSourcePathPolicy($"case policy '{policy.CasePolicy}'");
        }
    }

    static string IdentityPath(string path, DotNetSourcePathCasePolicy casePolicy)
    {
        if (casePolicy != DotNetSourcePathCasePolicy.InvariantLowercase)
        {
            return path;
        }

        try
        {
            return path.ToLowerInvariant().Normalize(NormalizationForm.FormC);
        }
        catch (ArgumentException)
        {
            throw new InvalidDotNetSourcePath(path);
        }
    }

    static string NormalizeProjectIdentity(string value)
    {
        string normalized;
        try
        {
            normalized = value?.Normalize(NormalizationForm.FormC).Trim() ?? string.Empty;
        }
        catch (ArgumentException)
        {
            throw new InvalidDotNetProjectIdentity(value ?? string.Empty);
        }

        if (string.IsNullOrWhiteSpace(normalized) || normalized.Any(char.IsControl))
        {
            throw new InvalidDotNetProjectIdentity(value ?? string.Empty);
        }

        return normalized;
    }

    static string NormalizeRelativePath(string value)
    {
        string normalized;
        try
        {
            normalized = value?.Normalize(NormalizationForm.FormC).Replace('\\', '/') ?? string.Empty;
        }
        catch (ArgumentException)
        {
            throw new InvalidDotNetSourcePath(value ?? string.Empty);
        }
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Any(char.IsControl) ||
            IsRooted(normalized))
        {
            throw new InvalidDotNetSourcePath(value ?? string.Empty);
        }

        var segments = new List<string>();
        foreach (var segment in normalized.Split('/'))
        {
            if (string.IsNullOrEmpty(segment) || segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                throw new InvalidDotNetSourcePath(value ?? string.Empty);
            }

            segments.Add(segment);
        }

        if (segments.Count == 0)
        {
            throw new InvalidDotNetSourcePath(value ?? string.Empty);
        }

        return string.Join('/', segments);
    }

    static bool IsRooted(string path) =>
        path[0] == '/' ||
        path[0] == '\\' ||
        (path.Length >= 2 && char.IsLetter(path[0]) && path[1] == ':');
}

/// <summary>
/// Contains the factory-created immutable source-file mapping snapshot for one analyzed project.
/// </summary>
public sealed class DotNetProjectSourceContext
{
    internal DotNetProjectSourceContext(
        string projectIdentity,
        DotNetSourcePathPolicy policy,
        IReadOnlyDictionary<SyntaxTree, DotNetSourceFile> files)
    {
        ProjectIdentity = projectIdentity;
        Policy = new()
        {
            Version = policy.Version,
            DisplayRoot = policy.DisplayRoot,
            CasePolicy = policy.CasePolicy
        };
        Files = new ReadOnlyDictionary<SyntaxTree, DotNetSourceFile>(new Dictionary<SyntaxTree, DotNetSourceFile>(files));
    }

    /// <summary>
    /// Gets the stable project identity used to qualify source files.
    /// </summary>
    public string ProjectIdentity { get; }

    /// <summary>
    /// Gets the explicit path policy.
    /// </summary>
    public DotNetSourcePathPolicy Policy { get; }

    /// <summary>
    /// Gets authored syntax-tree mappings.
    /// </summary>
    public IReadOnlyDictionary<SyntaxTree, DotNetSourceFile> Files { get; }
}

/// <summary>
/// The exception thrown when a source path is not a portable relative path.
/// </summary>
/// <param name="path">The invalid path.</param>
public sealed class InvalidDotNetSourcePath(string path) : Exception($"The source path '{path}' is not a portable relative path");

/// <summary>
/// The exception thrown when a project identity is empty or contains control characters.
/// </summary>
/// <param name="identity">The invalid identity.</param>
public sealed class InvalidDotNetProjectIdentity(string identity) : Exception($"The project identity '{identity}' is invalid");

/// <summary>
/// The exception thrown when a source-path policy is unsupported.
/// </summary>
/// <param name="policy">The unsupported policy description.</param>
public sealed class UnsupportedDotNetSourcePathPolicy(string policy) : Exception($"The .NET source-path policy {policy} is unsupported");

/// <summary>
/// The exception thrown when two documents resolve to the same stable source identity.
/// </summary>
/// <param name="identity">The duplicate identity.</param>
public sealed class DuplicateDotNetSourceIdentity(SourceFileIdentity identity) : Exception($"The source identity '{identity}' is duplicated");

/// <summary>
/// The exception thrown when one syntax tree is mapped more than once.
/// </summary>
/// <param name="path">The duplicated syntax-tree path.</param>
public sealed class DuplicateDotNetSourceTree(string path) : Exception($"The syntax tree '{path}' is mapped more than once");

/// <summary>
/// The exception thrown when an authored source tree has no host-supplied mapping.
/// </summary>
/// <param name="path">The unmapped syntax-tree path.</param>
public sealed class DotNetSourceTreeNotMapped(string path) : Exception($"The authored syntax tree '{path}' is not present in the source-path context");
