// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_direct_collection_initializer_entries_have_several_values : given.a_compilation
{
    DotNetBounded<DotNetCollectionValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/CollectionInitializers.cs",
            """
            using System.Collections.Generic;

            namespace Values;

            public sealed class SingleValueCollection : List<int>;

            public static class Usage
            {
                static int Helper() => 42;

                public static SingleValueCollection SingleValueEntry => new() { { 1 } };
                public static Dictionary<string, int> SeveralValues => new() { { "one", 1 } };
                public static Dictionary<string, int> SeveralValuesWithComputedChild => new() { { "one", Helper() } };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => DotNetSourceValues.Collection(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_preserve_a_direct_collection_initializer_entry_with_one_authored_value() => ((DotNetConstantValue)((DotNetKnown<DotNetCollectionValue>)_results[0]).Value.Values.Single().Value).Value.ShouldEqual(1);
    [Fact] void should_publish_no_empty_known_collection_for_a_multi_argument_add_entry() => _results.Skip(1).All(_ => _ is DotNetUnknown<DotNetCollectionValue>).ShouldBeTrue();
    [Fact] void should_reject_a_multi_argument_add_entry_at_the_nested_initializer() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_preserve_the_exact_nested_initializer_failure_location() => FailureSources(1).ShouldEqual(["{ \"one\", 1 }"]);
    [Fact] void should_inspect_every_multi_argument_add_child_after_the_shape_failure() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_order_the_nested_shape_before_its_computed_child_location() => FailureSources(2).ShouldEqual(["{ \"one\", Helper() }", "Helper()"]);

    DotNetValueFailureKind[] FailureKinds(int index) => [.. ((DotNetUnknown<DotNetCollectionValue>)_results[index]).Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. ((DotNetUnknown<DotNetCollectionValue>)_results[index]).Failures.Select(_ => _.Source.SourceTree!.GetText().ToString(_.Source.SourceSpan))];
}
