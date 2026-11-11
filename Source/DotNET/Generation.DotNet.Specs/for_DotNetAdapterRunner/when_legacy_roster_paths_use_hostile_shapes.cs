// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetAdapterRunner;

public class when_legacy_roster_paths_use_hostile_shapes : given.a_runner_context
{
    static readonly (string Label, string? Path)[] _cases =
    [
        ("plain", "a/Shared.csproj"),
        ("single-segment", "Shared.csproj"),
        ("backslash", "a\\Shared.csproj"),
        ("leading-dot", "./Shared.csproj"),
        ("inner-dot", "a/./Shared.csproj"),
        ("parent", "../Shared.csproj"),
        ("inner-parent", "a/../Shared.csproj"),
        ("double-separator", "a//Shared.csproj"),
        ("trailing-separator", "a/Shared.csproj/"),
        ("leading-separator", "/a/Shared.csproj"),
        ("drive-slash", "C:/a/Shared.csproj"),
        ("drive-relative", "c:Shared.csproj"),
        ("digit-drive", "1:/Shared.csproj"),
        ("unicode-drive", "\u00e9:/Shared.csproj"),
        ("encoded-dot", "%2e/Shared.csproj"),
        ("encoded-dot-dot", "%2e%2e/Shared.csproj"),
        ("recursively-encoded-dot-dot", "%252e%252e/Shared.csproj"),
        ("triply-encoded-dot-dot", "%25252e%25252e/Shared.csproj"),
        ("encoded-separator", "a%2FShared.csproj"),
        ("recursively-encoded-separator", "a%252FShared.csproj"),
        ("encoded-backslash", "a%5CShared.csproj"),
        ("encoded-drive-first-segment", "C%3A/Shared.csproj"),
        ("recursively-encoded-drive-first-segment", "C%253A/Shared.csproj"),
        ("encoded-drive-later-segment", "a/C%3A/Shared.csproj"),
        ("malformed-escape", "a%zz/Shared.csproj"),
        ("truncated-escape", "a%2/Shared.csproj"),
        ("lone-percent", "%/Shared.csproj"),
        ("leading-space", " a/Shared.csproj"),
        ("trailing-space", "a/Shared.csproj "),
        ("inner-space-segment", "a/ b/Shared.csproj"),
        ("control-tab", "a\t/Shared.csproj"),
        ("control-nul", "a\u0000/Shared.csproj"),
        ("unicode-precomposed", "\u00e9/Shared.csproj"),
        ("unicode-decomposed", "e\u0301/Shared.csproj"),
        ("unicode-letters", "caf\u00e9/\u65e5\u672c.csproj"),
        ("lone-surrogate", "\ud800/Shared.csproj"),
        ("empty", ""),
        ("null", null)
    ];

    string[] _results = null!;
    string _identicalPortable = null!;

    void Because()
    {
        _results = [.. _cases.Select(_ => $"{_.Label}|{Outcome(Run(_.Path, "other/Shared.csproj"))}")];
        _identicalPortable = Outcome(Run("a/Shared.csproj", "a/Shared.csproj"));
    }

    [Fact]
    void should_admit_or_block_every_hostile_shape_exactly() => string.Join('\n', _results).ShouldEqual(
        """
        plain|Admitted
        single-segment|Admitted
        backslash|Blocked|DOTNETADAPTER010
        leading-dot|Blocked|DOTNETADAPTER010
        inner-dot|Blocked|DOTNETADAPTER010
        parent|Blocked|DOTNETADAPTER010
        inner-parent|Blocked|DOTNETADAPTER010
        double-separator|Blocked|DOTNETADAPTER010
        trailing-separator|Blocked|DOTNETADAPTER010
        leading-separator|Blocked|DOTNETADAPTER010
        drive-slash|Blocked|DOTNETADAPTER010
        drive-relative|Blocked|DOTNETADAPTER010
        digit-drive|Admitted
        unicode-drive|Admitted
        encoded-dot|Blocked|DOTNETADAPTER010
        encoded-dot-dot|Blocked|DOTNETADAPTER010
        recursively-encoded-dot-dot|Blocked|DOTNETADAPTER010
        triply-encoded-dot-dot|Blocked|DOTNETADAPTER010
        encoded-separator|Blocked|DOTNETADAPTER010
        recursively-encoded-separator|Blocked|DOTNETADAPTER010
        encoded-backslash|Blocked|DOTNETADAPTER010
        encoded-drive-first-segment|Blocked|DOTNETADAPTER010
        recursively-encoded-drive-first-segment|Blocked|DOTNETADAPTER010
        encoded-drive-later-segment|Admitted
        malformed-escape|Admitted
        truncated-escape|Admitted
        lone-percent|Admitted
        leading-space|Blocked|DOTNETADAPTER010
        trailing-space|Blocked|DOTNETADAPTER010
        inner-space-segment|Admitted
        control-tab|Blocked|DOTNETADAPTER010
        control-nul|Blocked|DOTNETADAPTER010
        unicode-precomposed|Admitted
        unicode-decomposed|Blocked|DOTNETADAPTER010
        unicode-letters|Admitted
        lone-surrogate|Blocked|DOTNETADAPTER010
        empty|Blocked|DOTNETADAPTER010
        null|Blocked|DOTNETADAPTER010
        """);

    [Fact] void should_block_identical_portable_paths() => _identicalPortable.ShouldEqual("Blocked|DOTNETADAPTER010");

    static AdapterRunSnapshot Run(string? first, string? second) =>
        DotNetAdapterRunner.Run(
            [DotNetAdapterRegistration.For(new ModernAdapter(Descriptor("roster", language: AdapterSourceLanguage.CSharp)))],
            new DotNetAnalysisContext([Project(first), Project(second)]),
            Options);

    static DotNetProjectCompilation Project(string? projectPath) => new()
    {
        Name = "Shared.Project",
        ProjectPath = projectPath,
        Compilation = CSharpCompilation.Create("Shared"),
        AuthoredSyntaxTrees = Enumerable.Empty<SyntaxTree>().ToHashSet()
    };

    static string Outcome(AdapterRunSnapshot snapshot) =>
        $"{snapshot.Adapters.Single().Disposition}|{string.Join(',', snapshot.Diagnostics.Select(_ => _.Code))}"
            .TrimEnd('|');
}
