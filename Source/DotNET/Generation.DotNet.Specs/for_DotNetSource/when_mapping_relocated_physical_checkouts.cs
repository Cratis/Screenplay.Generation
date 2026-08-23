// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_relocated_physical_checkouts : given.a_compilation
{
    DotNetSourceFile _first = null!;
    DotNetSourceFile _relocated = null!;
    string _firstPhysicalPath = null!;
    string _relocatedPhysicalPath = null!;

    void Because()
    {
        var firstCompilation = CompilationFrom(new SourceFile(
            "/checkouts/first/apps/Banking/Account.cs",
            "namespace Banking; public record Account;"));
        var relocatedCompilation = CompilationFrom(new SourceFile(
            "/different/physical/root/apps/Banking/Account.cs",
            "namespace Banking; public record Account;"));
        var firstTree = firstCompilation.SyntaxTrees.Single();
        var relocatedTree = relocatedCompilation.SyntaxTrees.Single();

        _first = ContextFor(firstTree).Files[firstTree];
        _relocated = ContextFor(relocatedTree).Files[relocatedTree];
        _firstPhysicalPath = firstTree.FilePath;
        _relocatedPhysicalPath = relocatedTree.FilePath;
    }

    [Fact] void should_allow_roslyn_to_retain_distinct_physical_paths() => _firstPhysicalPath.ShouldNotEqual(_relocatedPhysicalPath);
    [Fact] void should_keep_identity_stable_across_relocation() => _first.Identity.ShouldEqual(_relocated.Identity);
    [Fact] void should_keep_display_path_stable_across_relocation() => _first.DisplayPath.ShouldEqual(_relocated.DisplayPath);
    [Fact]
    void should_not_retain_either_physical_root_in_stable_values() =>
        $"{_first.Identity}{_first.DisplayPath}{_relocated.Identity}{_relocated.DisplayPath}"
            .Contains("checkouts", StringComparison.Ordinal)
            .ShouldBeFalse();

    static DotNetProjectSourceContext ContextFor(SyntaxTree tree) => DotNetSourcePaths.Create(
        "Banking/Banking",
        new DotNetSourcePathPolicy
        {
            DisplayRoot = DotNetSourceDisplayRoot.Workspace,
            CasePolicy = DotNetSourcePathCasePolicy.Ordinal
        },
        [
            new DotNetSourceDocument
            {
                SyntaxTree = tree,
                ProjectRelativePath = "Account.cs",
                WorkspaceRelativePath = "apps/Banking/Account.cs"
            }
        ]);
}
