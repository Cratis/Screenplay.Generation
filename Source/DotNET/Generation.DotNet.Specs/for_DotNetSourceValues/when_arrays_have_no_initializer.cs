// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_arrays_have_no_initializer : given.a_compilation
{
    DotNetUnknown<DotNetCollectionValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/UninitializedArrays.cs",
            """
            namespace Values;

            public static class Usage
            {
                static int Size() => 2;

                public static int[] Fixed => new int[2];
                public static int[] Computed => new int[Size()];
                public static int[] Empty => new int[0];
                public static int[,] Multidimensional => new int[1, 2];
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => (DotNetUnknown<DotNetCollectionValue>)DotNetSourceValues.Collection(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_classify_every_array_without_an_initializer_as_collection_shaped_unknown() => _results.Length.ShouldEqual(4);
    [Fact] void should_report_the_whole_array_before_a_mismatched_dimension() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_whole_array_then_the_mismatched_dimension() => FailureSources(0).ShouldEqual(["new int[2]", "2"]);
    [Fact] void should_report_the_whole_array_before_a_computed_dimension() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_whole_array_then_the_computed_dimension() => FailureSources(1).ShouldEqual(["new int[Size()]", "Size()"]);
    [Fact] void should_not_synthesize_default_elements_for_an_empty_array() => FailureSources(2).ShouldEqual(["new int[0]"]);
    [Fact] void should_preserve_the_outer_rank_failure_after_the_whole_array() => FailureSources(3).ShouldEqual(["new int[1, 2]", "int[1, 2]"]);

    DotNetValueFailureKind[] FailureKinds(int index) => [.. _results[index].Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. _results[index].Failures.Select(_ => SourceText(_.Source))];

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
