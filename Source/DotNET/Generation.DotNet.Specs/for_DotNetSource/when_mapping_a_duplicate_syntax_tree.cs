// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_a_duplicate_syntax_tree : given.a_compilation
{
    Exception? _exception;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();

        _exception = Catch.Exception(() => DotNetSourcePaths.Create(
            "Orders",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                Document(tree, "Order.cs"),
                Document(tree, "Copies/Order.cs")
            ]));
    }

    [Fact] void should_reject_the_duplicate_tree_with_a_typed_exception() => _exception.ShouldBeOfExactType<DuplicateDotNetSourceTree>();

    static DotNetSourceDocument Document(SyntaxTree tree, string path) => new()
    {
        SyntaxTree = tree,
        ProjectRelativePath = path,
        WorkspaceRelativePath = path
    };
}
