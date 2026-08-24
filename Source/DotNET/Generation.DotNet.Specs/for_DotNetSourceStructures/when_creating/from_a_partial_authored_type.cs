// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class from_a_partial_authored_type : given.a_compilation
{
    DotNetSourceStructureSnapshot _snapshot = null!;
    DotNetSourceStructure _structure = null!;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile(
                "/checkout/Banking/Source/Accounts/Register/Register.cs",
                "namespace Banking.Accounts.Register; public partial record Register;"),
            new SourceFile(
                "/checkout/Banking/Source/Accounts/Register/Register.Validation.cs",
                "namespace Banking.Accounts.Register; public partial record Register;"));
        var trees = compilation.SyntaxTrees.ToArray();
        var sourceContext = DotNetSourcePaths.Create(
            "apps/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                Document(trees[0], "Source/Accounts/Register/Register.cs"),
                Document(trees[1], "Source/Accounts/Register/Register.Validation.cs")
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Role = DotNetProjectRole.Application,
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = trees.ToHashSet()
        };

        _snapshot = DotNetSourceStructures.Create(new DotNetAnalysisContext([project]));
        _structure = _snapshot.Structures.Single();
    }

    [Fact] void should_succeed() => _snapshot.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_project_identity() => _structure.Project.ShouldEqual("apps/Banking");
    [Fact] void should_preserve_the_project_role() => _structure.ProjectRole.ShouldEqual(DotNetProjectRole.Application);
    [Fact] void should_preserve_the_exact_namespace() => _structure.Namespace.ShouldEqual("Banking.Accounts.Register");
    [Fact] void should_order_every_authored_declaration_path() => string.Join('|', _structure.ProjectRelativePaths).ShouldEqual(
        "Source/Accounts/Register/Register.Validation.cs|Source/Accounts/Register/Register.cs");
    [Fact] void should_preserve_stable_source_identity() => _structure.Source!.FileIdentity!.Project.ShouldEqual("apps/Banking");

    static DotNetSourceDocument Document(SyntaxTree tree, string path) => new()
    {
        SyntaxTree = tree,
        ProjectRelativePath = path,
        WorkspaceRelativePath = $"apps/Banking/{path}"
    };
}
