// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

sealed class DotNetSourceAuthorityValidator(DotNetAnalysisContext context, bool requiresStableIdentity) : ISourceAuthorityValidator
{
    readonly bool _requiresStableIdentity = requiresStableIdentity;
    readonly IReadOnlyList<AuthoritativeSource> _sources =
    [
        .. context.Projects.SelectMany(SourcesIn)
            .OrderBy(source => source.Identity?.Project, StringComparer.Ordinal)
            .ThenBy(source => source.Identity?.Path, StringComparer.Ordinal)
            .ThenBy(source => source.DisplayPath, StringComparer.Ordinal)
    ];

    public bool IsAuthoritative(SourceRange source)
    {
        var isOrdered = source.EndLine > source.StartLine ||
                        (source.EndLine == source.StartLine && source.EndColumn >= source.StartColumn);
        if ((_requiresStableIdentity && source.FileIdentity is null) ||
            !IsPortableRelativePath(source.Path) ||
            !isOrdered)
        {
            return false;
        }

        var candidates = _sources.Where(candidate => Matches(candidate, source)).ToArray();
        return candidates.Length == 1 && CoordinatesAreValid(source, candidates[0].SyntaxTree);
    }

    static IEnumerable<AuthoritativeSource> SourcesIn(DotNetProjectCompilation project)
    {
        if (project.AuthoredSyntaxTrees is null)
        {
            yield break;
        }

        var compilationTrees = project.Compilation.SyntaxTrees.ToHashSet();
        foreach (var tree in project.AuthoredSyntaxTrees.Where(compilationTrees.Contains))
        {
            if (DotNetGeneratedSource.IsGenerated(tree))
            {
                continue;
            }

            if (project.SourceContext is not null)
            {
                if (project.SourceContext.Files.TryGetValue(tree, out var file))
                {
                    yield return new AuthoritativeSource(tree, file.DisplayPath, file.Identity);
                }

                continue;
            }

            yield return new AuthoritativeSource(tree, LegacyDisplayPath(project, tree), null);
        }
    }

    static bool Matches(AuthoritativeSource candidate, SourceRange source)
    {
        if (!string.Equals(candidate.DisplayPath, source.Path, StringComparison.Ordinal))
        {
            return false;
        }

        return source.FileIdentity is null || candidate.Identity == source.FileIdentity;
    }

    static bool CoordinatesAreValid(SourceRange source, SyntaxTree tree)
    {
        var lines = tree.GetText().Lines;
        return PositionIsValid(source.StartLine, source.StartColumn, lines) &&
               PositionIsValid(source.EndLine, source.EndColumn, lines);
    }

    static bool PositionIsValid(int line, int column, Microsoft.CodeAnalysis.Text.TextLineCollection lines) =>
        line >= 1 &&
        line <= lines.Count &&
        column >= 1 &&
        column <= lines[line - 1].Span.Length + 1;

    static string LegacyDisplayPath(DotNetProjectCompilation project, SyntaxTree tree)
    {
        var path = tree.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        string displayPath;
        if (string.IsNullOrWhiteSpace(project.SourceRoot) || !Path.IsPathFullyQualified(path))
        {
            displayPath = path.Replace('\\', '/');
        }
        else
        {
            var relative = Path.GetRelativePath(project.SourceRoot, path).Replace('\\', '/');
            displayPath = relative == ".." || relative.StartsWith("../", StringComparison.Ordinal)
                ? Path.GetFileName(path)
                : relative;
        }

        return IsPortableRelativePath(displayPath) ? displayPath : string.Empty;
    }

    static bool IsPortableRelativePath(string? value)
    {
        if (string.IsNullOrEmpty(value) ||
            !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            value.Any(char.IsControl) ||
            value[0] == '/' ||
            value.Contains('\\') ||
            IsDriveRooted(value))
        {
            return false;
        }

        try
        {
            if (!string.Equals(value, value.Normalize(), StringComparison.Ordinal))
            {
                return false;
            }
        }
        catch (ArgumentException)
        {
            return false;
        }

        var segments = value.Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (string.IsNullOrEmpty(segments[index]) ||
                !TryDecodeSegment(segments[index], out var decoded) ||
                string.Equals(decoded, ".", StringComparison.Ordinal) ||
                string.Equals(decoded, "..", StringComparison.Ordinal) ||
                decoded.Contains('/') ||
                decoded.Contains('\\') ||
                (index == 0 && IsDriveRooted(decoded)))
            {
                return false;
            }
        }

        return true;
    }

    static bool TryDecodeSegment(string segment, out string decoded)
    {
        decoded = segment;
        try
        {
            while (true)
            {
                var unescaped = Uri.UnescapeDataString(decoded);
                if (string.Equals(unescaped, decoded, StringComparison.Ordinal))
                {
                    return true;
                }

                decoded = unescaped;
            }
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    static bool IsDriveRooted(string path) =>
        path.Length >= 2 && char.IsAsciiLetter(path[0]) && path[1] == ':';

    sealed record AuthoritativeSource(
        SyntaxTree SyntaxTree,
        string DisplayPath,
        SourceFileIdentity? Identity);
}
