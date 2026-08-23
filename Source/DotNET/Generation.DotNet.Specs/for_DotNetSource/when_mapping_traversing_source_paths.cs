// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_mapping_traversing_source_paths : given.a_compilation
{
    Exception? _leadingException;
    Exception? _internalException;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/checkout/Order.cs", "public record Order;"));
        var tree = compilation.SyntaxTrees.Single();
        _leadingException = ExceptionFor(tree, "../Order.cs");
        _internalException = ExceptionFor(tree, "Features/../Order.cs");
    }

    [Fact] void should_reject_leading_traversal() => _leadingException.ShouldBeOfExactType<InvalidDotNetSourcePath>();
    [Fact] void should_reject_internal_traversal() => _internalException.ShouldBeOfExactType<InvalidDotNetSourcePath>();

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
