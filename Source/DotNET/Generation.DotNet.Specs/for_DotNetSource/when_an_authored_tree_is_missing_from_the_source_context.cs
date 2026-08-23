// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSource;

public class when_an_authored_tree_is_missing_from_the_source_context : given.a_compilation
{
    Exception? _exception;

    void Because()
    {
        var compilation = CompilationFrom(
            new SourceFile("/checkout/Mapped.cs", "public record Mapped;"),
            new SourceFile("/checkout/Unmapped.cs", "public record Unmapped;"));
        var trees = compilation.SyntaxTrees.ToArray();
        var sourceContext = DotNetSourcePaths.Create(
            "Orders",
            new DotNetSourcePathPolicy
            {
                DisplayRoot = DotNetSourceDisplayRoot.Project,
                CasePolicy = DotNetSourcePathCasePolicy.Ordinal
            },
            [
                new DotNetSourceDocument
                {
                    SyntaxTree = trees[0],
                    ProjectRelativePath = "Mapped.cs",
                    WorkspaceRelativePath = "Mapped.cs"
                }
            ]);
        var project = new DotNetProjectCompilation
        {
            Name = "Orders",
            Compilation = compilation,
            SourceContext = sourceContext,
            AuthoredSyntaxTrees = trees.ToHashSet()
        };

        var rangeForProject = typeof(DotNetSource).GetMethod("RangeForProject");
        _exception = Catch.Exception(() => rangeForProject.Invoke(null, [trees[1].GetRoot().GetLocation(), project]));
    }

    [Fact] void should_fail_with_a_typed_mapping_exception() => (_exception as TargetInvocationException).InnerException.ShouldBeOfExactType<DotNetSourceTreeNotMapped>();
}
