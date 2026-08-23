// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_unsupported_source_path_policies : given.a_compilation
{
    Exception?[] _exceptions = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();
        _exceptions =
        [
            ExceptionFor(tree, new DotNetSourcePathPolicy
            {
                Version = 2,
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            }),
            ExceptionFor(tree, new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Unknown,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            }),
            ExceptionFor(tree, new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Unknown
            })
        ];
    }

    [Fact]
    void should_reject_unknown_version_display_and_case_policies() =>
        _exceptions.All(_ => _ is UnsupportedDotNetSourcePathPolicy).ShouldBeTrue();

    static Exception? ExceptionFor(SyntaxTree tree, DotNetSourcePathPolicy policy) => Catch.Exception(() => DotNetSourcePaths.Create(
        "Orders",
        policy,
        [
            new DotNetSourceDocument
            {
                SyntaxTree = tree,
                ProjectRelativePath = "Order.cs",
                WorkspaceRelativePath = "Order.cs"
            }
        ]));
}
