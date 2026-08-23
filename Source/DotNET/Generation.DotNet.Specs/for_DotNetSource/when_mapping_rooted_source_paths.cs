// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_rooted_source_paths : given.a_compilation
{
    Exception?[] _exceptions = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();
        _exceptions =
        [
            ExceptionFor(tree, "/checkout/Order.cs"),
            ExceptionFor(tree, "C:\\checkout\\Order.cs"),
            ExceptionFor(tree, "\\\\server\\share\\Order.cs")
        ];
    }

    [Fact]
    void should_reject_unix_drive_and_unc_roots_with_typed_exceptions() =>
        _exceptions.All(_ => _ is InvalidDotNetSourcePath).ShouldBeTrue();

    static Exception? ExceptionFor(SyntaxTree tree, string path) => Catch.Exception(() => DotNetSourcePaths.Create(
        "Orders",
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
                WorkspaceRelativePath = "Order.cs"
            }
        ]));
}
