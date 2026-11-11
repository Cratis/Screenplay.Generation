// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_source_authority_meets_hostile_shapes : given.a_runner_context
{
    const string Content = "public class Code { }\n";

    static readonly (string Label, string TreePath)[] _treePaths =
    [
        ("plain", "a/b.cs"),
        ("backslash", "a\\b.cs"),
        ("leading-dot", "./a.cs"),
        ("parent", "../a.cs"),
        ("inner-parent", "a/../b.cs"),
        ("double-separator", "a//b.cs"),
        ("trailing-separator", "a/b/"),
        ("rooted", "/a.cs"),
        ("drive-slash", "C:/a.cs"),
        ("drive-backslash", "C:\\a.cs"),
        ("digit-drive", "1:/a.cs"),
        ("encoded-dot-dot", "%2e%2e/a.cs"),
        ("recursively-encoded-dot-dot", "%252e%252e/a.cs"),
        ("triply-encoded-dot-dot", "%25252e%25252e/a.cs"),
        ("encoded-separator", "a%2Fb.cs"),
        ("recursively-encoded-separator", "a%252Fb.cs"),
        ("encoded-backslash", "a%5Cb.cs"),
        ("encoded-drive-first-segment", "C%3A/a.cs"),
        ("encoded-drive-later-segment", "a/C%3A/b.cs"),
        ("malformed-escape", "a%zz/b.cs"),
        ("lone-percent", "%/b.cs"),
        ("leading-space", " a.cs"),
        ("trailing-space", "a.cs "),
        ("inner-space-segment", "a/ b.cs"),
        ("control-tab", "a\tb.cs"),
        ("unicode-precomposed", "\u00e9/a.cs"),
        ("unicode-decomposed", "e\u0301/a.cs"),
        ("unicode-letters", "caf\u00e9/\u65e5\u672c.cs"),
        ("lone-surrogate", "\ud800/a.cs"),
        ("empty", "")
    ];

    static readonly (string Label, int StartLine, int StartColumn, int EndLine, int EndColumn)[] _coordinates =
    [
        ("in-range", 1, 1, 1, 7),
        ("empty-span", 1, 3, 1, 3),
        ("end-column-before-start", 1, 5, 1, 2),
        ("end-line-before-start", 2, 1, 1, 7),
        ("multi-line", 1, 1, 2, 1),
        ("line-after-last", 1, 1, 3, 1),
        ("line-zero", 0, 1, 1, 7),
        ("column-zero", 1, 0, 1, 7),
        ("negative-line", -1, 1, 1, 7),
        ("negative-column", 1, -1, 1, 7),
        ("end-of-line", 1, 1, 1, 22),
        ("past-end-of-line", 1, 1, 1, 23),
        ("empty-last-line", 2, 1, 2, 1),
        ("past-empty-last-line", 2, 1, 2, 2)
    ];

    string[] _legacyRaw = null!;
    string[] _legacyDisplay = null!;
    string[] _coordinateResults = null!;
    string _missingIdentity = null!;
    string _wrongIdentity = null!;
    string _modernEncodedDotDot = null!;
    string _modernLeadingSpace = null!;
    string _modernPlain = null!;

    void Because()
    {
        _legacyRaw = [.. _treePaths.Select(_ => $"{_.Label}|{Authoritative(LegacyProject(_.TreePath), _.TreePath, null)}")];
        _legacyDisplay = [.. _treePaths.Select(_ => $"{_.Label}|{Authoritative(LegacyProject(_.TreePath), _.TreePath.Replace('\\', '/'), null)}")];
        var legacy = LegacyProject("a/b.cs");
        _coordinateResults = [.. _coordinates.Select(_ => $"{_.Label}|{Authoritative(legacy, "a/b.cs", null, _.StartLine, _.StartColumn, _.EndLine, _.EndColumn)}")];

        var plain = MappedProject("project", "Project", "Project", "/checkout/Code.cs", Content, "a/b.cs");
        var plainIdentity = plain.SourceContext!.Files.Values.Single().Identity;
        _modernPlain = Authoritative(plain, "a/b.cs", plainIdentity);
        _missingIdentity = Authoritative(plain, "a/b.cs", null, stable: true);
        _wrongIdentity = Authoritative(plain, "a/b.cs", new SourceFileIdentity { Project = "project", Path = "other.cs" });

        var encoded = MappedProject("project", "Project", "Project", "/checkout/Code.cs", Content, "%2e%2e/a.cs");
        _modernEncodedDotDot = Authoritative(encoded, "%2e%2e/a.cs", encoded.SourceContext!.Files.Values.Single().Identity);
        var spaced = MappedProject("project", "Project", "Project", "/checkout/Code.cs", Content, " a.cs");
        _modernLeadingSpace = Authoritative(spaced, " a.cs", spaced.SourceContext!.Files.Values.Single().Identity);
    }

    [Fact]
    void should_judge_every_hostile_raw_legacy_range_path_exactly() => string.Join('\n', _legacyRaw).ShouldEqual(
        """
        plain|authoritative
        backslash|rejected
        leading-dot|rejected
        parent|rejected
        inner-parent|rejected
        double-separator|rejected
        trailing-separator|rejected
        rooted|rejected
        drive-slash|rejected
        drive-backslash|rejected
        digit-drive|authoritative
        encoded-dot-dot|rejected
        recursively-encoded-dot-dot|rejected
        triply-encoded-dot-dot|rejected
        encoded-separator|rejected
        recursively-encoded-separator|rejected
        encoded-backslash|rejected
        encoded-drive-first-segment|rejected
        encoded-drive-later-segment|authoritative
        malformed-escape|authoritative
        lone-percent|authoritative
        leading-space|rejected
        trailing-space|rejected
        inner-space-segment|authoritative
        control-tab|rejected
        unicode-precomposed|authoritative
        unicode-decomposed|rejected
        unicode-letters|authoritative
        lone-surrogate|rejected
        empty|rejected
        """);

    [Fact]
    void should_judge_every_hostile_legacy_display_path_exactly() => string.Join('\n', _legacyDisplay).ShouldEqual(
        """
        plain|authoritative
        backslash|authoritative
        leading-dot|rejected
        parent|rejected
        inner-parent|rejected
        double-separator|rejected
        trailing-separator|rejected
        rooted|rejected
        drive-slash|rejected
        drive-backslash|rejected
        digit-drive|authoritative
        encoded-dot-dot|rejected
        recursively-encoded-dot-dot|rejected
        triply-encoded-dot-dot|rejected
        encoded-separator|rejected
        recursively-encoded-separator|rejected
        encoded-backslash|rejected
        encoded-drive-first-segment|rejected
        encoded-drive-later-segment|authoritative
        malformed-escape|authoritative
        lone-percent|authoritative
        leading-space|rejected
        trailing-space|rejected
        inner-space-segment|authoritative
        control-tab|rejected
        unicode-precomposed|authoritative
        unicode-decomposed|rejected
        unicode-letters|authoritative
        lone-surrogate|rejected
        empty|rejected
        """);

    [Fact]
    void should_judge_every_coordinate_shape_exactly() => string.Join('\n', _coordinateResults).ShouldEqual(
        """
        in-range|authoritative
        empty-span|authoritative
        end-column-before-start|rejected
        end-line-before-start|rejected
        multi-line|authoritative
        line-after-last|rejected
        line-zero|rejected
        column-zero|rejected
        negative-line|rejected
        negative-column|rejected
        end-of-line|authoritative
        past-end-of-line|rejected
        empty-last-line|authoritative
        past-empty-last-line|rejected
        """);

    [Fact] void should_admit_a_mapped_plain_path_with_its_identity() => _modernPlain.ShouldEqual("authoritative");
    [Fact] void should_reject_a_stable_range_without_identity() => _missingIdentity.ShouldEqual("rejected");
    [Fact] void should_reject_a_stable_range_with_another_identity() => _wrongIdentity.ShouldEqual("rejected");
    [Fact] void should_reject_a_mapped_path_that_decodes_to_traversal() => _modernEncodedDotDot.ShouldEqual("rejected");
    [Fact] void should_reject_a_mapped_path_with_a_leading_space() => _modernLeadingSpace.ShouldEqual("rejected");

    static string Authoritative(
        DotNetProjectCompilation project,
        string path,
        SourceFileIdentity? identity,
        int startLine = 1,
        int startColumn = 1,
        int endLine = 1,
        int endColumn = 7,
        bool stable = false)
    {
        var adapter = new ModernAdapter(Descriptor(
            "authority",
            language: AdapterSourceLanguage.CSharp,
            hostCapabilities: stable || identity is not null
                ? [AdapterHostCapability.AuthoredSource, AdapterHostCapability.StableSourceLocations, AdapterHostCapability.SemanticAnalysis]
                : [AdapterHostCapability.AuthoredSource, AdapterHostCapability.SemanticAnalysis]))
        {
            ProbeResult = new AdapterProbeApplicable
            {
                Evidence =
                [
                    new AdapterProbeEvidence
                    {
                        Description = "The exact API is present",
                        Source = new SourceRange
                        {
                            Path = path,
                            FileIdentity = identity,
                            StartLine = startLine,
                            StartColumn = startColumn,
                            EndLine = endLine,
                            EndColumn = endColumn
                        }
                    }
                ]
            }
        };
        var record = DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(adapter)],
            new DotNetAnalysisContext([project]),
            Options).Adapters.Single();
        return record.Disposition == AdapterRunDisposition.Blocked ? "rejected" : "authoritative";
    }

    static DotNetProjectCompilation LegacyProject(string treePath)
    {
        var tree = CSharpSyntaxTree.ParseText(
            Content,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview),
            treePath);
        return new DotNetProjectCompilation
        {
            Name = "Project",
            Compilation = CSharpCompilation.Create(
                "Project",
                [tree],
                CompilationFrom().References,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)),
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };
    }
}
