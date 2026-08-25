// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceStructures.when_creating;

public class with_case_folded_source_identity : given.a_compilation
{
    DotNetSourceFile _sourceFile = null!;
    DotNetSourceStructure _structure = null!;
    DotNetSourceStructureResolution _resolution = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/physical-checkout/Banking/Source/Accounts/Register/Register.cs",
            "namespace Banking.Accounts.Register; public record Register;"));
        var tree = compilation.SyntaxTrees.Single();
        var sourceContext = DotNetSourcePaths.Create(
            "apps/Banking",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Workspace,
                CasePolicy = DotNetSourcePathCasePolicy.InvariantLowercase
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = tree,
                    ProjectRelativePath = "Source/Accounts/Register/Register.cs",
                    WorkspaceRelativePath = "apps/Banking/Source/Accounts/Register/Register.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = new HashSet<SyntaxTree> { tree }
        };

        _sourceFile = sourceContext.Files[tree];
        _structure = DotNetSourceStructures.Create(new DotNetAnalysisContext([project])).Structures.Single();
        _resolution = DotNetSourceStructureResolver.Resolve(
            _structure,
            GenerationSliceKind.StateChange,
            new DotNetSourceStructurePolicy
            {
                FeatureRoot = "Source",
                NamespaceSegmentsToSkip = 1
            });
    }

    [Fact] void should_fold_only_the_stable_file_identity() => _sourceFile.Identity.Path.ShouldEqual("source/accounts/register/register.cs");
    [Fact] void should_preserve_project_relative_path_casing() => _sourceFile.ProjectRelativePath.ShouldEqual("Source/Accounts/Register/Register.cs");
    [Fact] void should_keep_workspace_display_policy_independent() => _sourceFile.DisplayPath.ShouldEqual("apps/Banking/Source/Accounts/Register/Register.cs");
    [Fact] void should_snapshot_the_casing_preserving_path() => _structure.ProjectRelativePaths.Single().ShouldEqual("Source/Accounts/Register/Register.cs");
    [Fact] void should_resolve_without_a_false_structure_conflict() => _resolution.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_authored_module_name() => _resolution.Placement.Module.ShouldEqual("Accounts");
    [Fact] void should_preserve_the_authored_slice_name() => _resolution.Placement.Slice.ShouldEqual("Register");
}
