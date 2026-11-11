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
        if (!HasAcceptableShape(source))
        {
            return false;
        }

        var candidates = _sources.Where(candidate => Matches(candidate, source)).ToArray();
        return candidates.Length == 1 && CoordinatesAreValid(source, candidates[0].SyntaxTree);
    }

    static bool IsOrdered(SourceRange source) =>
        source.EndLine > source.StartLine ||
        (source.EndLine == source.StartLine && source.EndColumn >= source.StartColumn);

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

            var source = SourceOf(project, tree);
            if (source is not null)
            {
                yield return source;
            }
        }
    }

    static AuthoritativeSource? SourceOf(DotNetProjectCompilation project, SyntaxTree tree)
    {
        if (project.SourceContext is null)
        {
            return new AuthoritativeSource(tree, LegacyDisplayPath(project, tree), null);
        }

        return project.SourceContext.Files.TryGetValue(tree, out var file)
            ? new AuthoritativeSource(tree, file.DisplayPath, file.Identity)
            : null;
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

    static bool IsPortableRelativePath(string? value) =>
        value is { Length: > 0 } &&
        HasPortableShape(value) &&
        IsNormalized(value) &&
        value.Split('/').Select(IsPortableSegment).All(portable => portable);

    static bool HasPortableShape(string value) =>
        string.Equals(value, value.Trim(), StringComparison.Ordinal) &&
        !value.Any(char.IsControl) &&
        value[0] != '/' &&
        !value.Contains('\\') &&
        !IsDriveRooted(value);

    static bool IsNormalized(string value)
    {
        try
        {
            return string.Equals(value, value.Normalize(), StringComparison.Ordinal);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    static bool IsPortableSegment(string segment, int index) =>
        !string.IsNullOrEmpty(segment) &&
        TryDecodeSegment(segment, out var decoded) &&
        !IsTraversal(decoded) &&
        !decoded.AsSpan().ContainsAny('/', '\\') &&
        !(index == 0 && IsDriveRooted(decoded));

    static bool IsTraversal(string decoded) =>
        string.Equals(decoded, ".", StringComparison.Ordinal) ||
        string.Equals(decoded, "..", StringComparison.Ordinal);

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

    bool HasAcceptableShape(SourceRange source) =>
        !(_requiresStableIdentity && source.FileIdentity is null) &&
        IsPortableRelativePath(source.Path) &&
        IsOrdered(source);

    sealed record AuthoritativeSource(
        SyntaxTree SyntaxTree,
        string DisplayPath,
        SourceFileIdentity? Identity);
}
