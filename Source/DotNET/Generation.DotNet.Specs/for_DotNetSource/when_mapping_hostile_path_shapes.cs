// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_hostile_path_shapes : Specification
{
    static readonly (string Label, string? Path)[] _cases =
    [
        ("plain", "a/b.cs"),
        ("backslash", "a\\b.cs"),
        ("mixed-separators", "a\\b/c.cs"),
        ("leading-dot", "./a.cs"),
        ("inner-dot", "a/./b.cs"),
        ("double-separator", "a//b.cs"),
        ("trailing-separator", "a/b/"),
        ("trailing-backslash", "a\\b\\"),
        ("only-dot", "."),
        ("only-dot-separator", "./"),
        ("parent", "../a.cs"),
        ("inner-parent", "a/../b.cs"),
        ("trailing-parent", "a/.."),
        ("backslash-parent", "a\\..\\b.cs"),
        ("encoded-dot-dot", "%2e%2e/a.cs"),
        ("recursively-encoded-dot-dot", "%252e%252e/a.cs"),
        ("encoded-separator", "a%2Fb.cs"),
        ("encoded-backslash", "a%5Cb.cs"),
        ("encoded-drive", "C%3A/a.cs"),
        ("rooted", "/a.cs"),
        ("rooted-backslash", "\\a.cs"),
        ("unc", "\\\\server\\share\\a.cs"),
        ("drive-slash", "C:/a.cs"),
        ("drive-backslash", "C:\\a.cs"),
        ("drive-relative", "c:a.cs"),
        ("digit-drive", "1:/a.cs"),
        ("unicode-drive", "\u00e9:/a.cs"),
        ("empty", ""),
        ("null", null),
        ("whitespace", "   "),
        ("leading-space", " a.cs"),
        ("trailing-space", "a.cs "),
        ("inner-space-segment", "a/ b.cs"),
        ("dot-dot-with-space", "a/.. /b.cs"),
        ("three-dots", "a/.../b.cs"),
        ("control-nul", "a\u0000b.cs"),
        ("control-tab", "a\tb.cs"),
        ("control-newline", "a\nb.cs"),
        ("unicode-precomposed", "\u00e9/a.cs"),
        ("unicode-decomposed", "e\u0301/a.cs"),
        ("unicode-letters", "caf\u00e9/\u65e5\u672c.cs"),
        ("lone-surrogate", "\ud800/a.cs")
    ];

    string[] _results = null!;

    void Because() => _results = [.. _cases.Select(_ => $"{_.Label}|{Map(_.Path)}")];

    [Fact]
    void should_map_or_reject_every_hostile_shape_exactly() => string.Join('\n', _results).ShouldEqual(
        """
        plain|a/b.cs|a/b.cs|a/b.cs
        backslash|a/b.cs|a/b.cs|a/b.cs
        mixed-separators|a/b/c.cs|a/b/c.cs|a/b/c.cs
        leading-dot|a.cs|a.cs|a.cs
        inner-dot|a/b.cs|a/b.cs|a/b.cs
        double-separator|a/b.cs|a/b.cs|a/b.cs
        trailing-separator|a/b|a/b|a/b
        trailing-backslash|a/b|a/b|a/b
        only-dot|InvalidDotNetSourcePath
        only-dot-separator|InvalidDotNetSourcePath
        parent|InvalidDotNetSourcePath
        inner-parent|InvalidDotNetSourcePath
        trailing-parent|InvalidDotNetSourcePath
        backslash-parent|InvalidDotNetSourcePath
        encoded-dot-dot|%2e%2e/a.cs|%2e%2e/a.cs|%2e%2e/a.cs
        recursively-encoded-dot-dot|%252e%252e/a.cs|%252e%252e/a.cs|%252e%252e/a.cs
        encoded-separator|a%2Fb.cs|a%2Fb.cs|a%2Fb.cs
        encoded-backslash|a%5Cb.cs|a%5Cb.cs|a%5Cb.cs
        encoded-drive|C%3A/a.cs|C%3A/a.cs|C%3A/a.cs
        rooted|InvalidDotNetSourcePath
        rooted-backslash|InvalidDotNetSourcePath
        unc|InvalidDotNetSourcePath
        drive-slash|InvalidDotNetSourcePath
        drive-backslash|InvalidDotNetSourcePath
        drive-relative|InvalidDotNetSourcePath
        digit-drive|1:/a.cs|1:/a.cs|1:/a.cs
        unicode-drive|InvalidDotNetSourcePath
        empty|InvalidDotNetSourcePath
        null|InvalidDotNetSourcePath
        whitespace|InvalidDotNetSourcePath
        leading-space| a.cs| a.cs| a.cs
        trailing-space|a.cs |a.cs |a.cs 
        inner-space-segment|a/ b.cs|a/ b.cs|a/ b.cs
        dot-dot-with-space|a/.. /b.cs|a/.. /b.cs|a/.. /b.cs
        three-dots|a/.../b.cs|a/.../b.cs|a/.../b.cs
        control-nul|InvalidDotNetSourcePath
        control-tab|InvalidDotNetSourcePath
        control-newline|InvalidDotNetSourcePath
        unicode-precomposed|\u00e9/a.cs|\u00e9/a.cs|\u00e9/a.cs
        unicode-decomposed|\u00e9/a.cs|\u00e9/a.cs|\u00e9/a.cs
        unicode-letters|caf\u00e9/\u65e5\u672c.cs|caf\u00e9/\u65e5\u672c.cs|caf\u00e9/\u65e5\u672c.cs
        lone-surrogate|InvalidDotNetSourcePath
        """);

    static string Map(string? path)
    {
        var tree = CSharpSyntaxTree.ParseText("class A { }");
        try
        {
            var context = DotNetSourcePaths.Create(
                "Project",
                new DotNetSourcePathPolicy
                {
                    DisplayRoot = DotNetSourceDisplayRoot.Project,
                    CasePolicy = DotNetSourcePathCasePolicy.Ordinal
                },
                [
                    new DotNetSourceDocument
                    {
                        SyntaxTree = tree,
                        ProjectRelativePath = path,
                        WorkspaceRelativePath = path
                    }
                ]);
            var file = context.Files.Values.Single();
            return $"{Escape(file.ProjectRelativePath)}|{Escape(file.DisplayPath)}|{Escape(file.Identity.Path)}";
        }
        catch (InvalidDotNetSourcePath)
        {
            return "InvalidDotNetSourcePath";
        }
    }

    static string Escape(string value) => string.Concat(value.Select(character =>
        char.IsControl(character) || char.IsSurrogate(character) || character > 0x7e
            ? $"\\u{(int)character:x4}"
            : character.ToString()));
}
