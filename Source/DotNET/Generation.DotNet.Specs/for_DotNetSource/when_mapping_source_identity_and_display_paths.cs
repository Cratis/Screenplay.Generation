// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_source_identity_and_display_paths : given.a_compilation
{
    DotNetSourceFile _workspaceFile = null!;
    DotNetSourceFile _projectFile = null!;
    DotNetSourceFile _otherProjectFile = null!;

    void Because()
    {
        var firstCompilation = CompilationFrom(new SourceFile(
            "/first-checkout/apps/Banking/Common/Order.cs",
            "namespace Banking; public record Order;"));
        var secondCompilation = CompilationFrom(new SourceFile(
            "/second-checkout/apps/Shipping/Common/Order.cs",
            "namespace Shipping; public record Order;"));
        var firstTree = firstCompilation.SyntaxTrees.Single();
        var secondTree = secondCompilation.SyntaxTrees.Single();
        var workspaceContext = DotNetSourcePaths.Create(
            "Banking/Banking",
            Policy(DotNetSourceDisplayRoot.Workspace),
            [Document(firstTree, "Common/Order.cs", "apps/Banking/Common/Order.cs")]);
        var projectContext = DotNetSourcePaths.Create(
            "Banking/Banking",
            Policy(DotNetSourceDisplayRoot.Project),
            [Document(firstTree, "Common/Order.cs", "apps/Banking/Common/Order.cs")]);
        var otherContext = DotNetSourcePaths.Create(
            "Shipping/Shipping",
            Policy(DotNetSourceDisplayRoot.Workspace),
            [Document(secondTree, "Common/Order.cs", "apps/Shipping/Common/Order.cs")]);

        _workspaceFile = workspaceContext.Files[firstTree];
        _projectFile = projectContext.Files[firstTree];
        _otherProjectFile = otherContext.Files[secondTree];
    }

    [Fact] void should_keep_identity_independent_from_display_root() => _workspaceFile.Identity.ShouldEqual(_projectFile.Identity);
    [Fact] void should_display_the_workspace_relative_path() => _workspaceFile.DisplayPath.ShouldEqual("apps/Banking/Common/Order.cs");
    [Fact] void should_display_the_project_relative_path() => _projectFile.DisplayPath.ShouldEqual("Common/Order.cs");
    [Fact] void should_disambiguate_the_same_relative_path_in_another_project() => _workspaceFile.Identity.ShouldNotEqual(_otherProjectFile.Identity);

    static DotNetSourcePathPolicy Policy(DotNetSourceDisplayRoot displayRoot) => new()
    {
        DisplayRoot = displayRoot,
        CasePolicy = DotNetSourcePathCasePolicy.Ordinal
    };

    static DotNetSourceDocument Document(SyntaxTree tree, string projectPath, string workspacePath) => new()
    {
        SyntaxTree = tree,
        ProjectRelativePath = projectPath,
        WorkspaceRelativePath = workspacePath
    };
}
