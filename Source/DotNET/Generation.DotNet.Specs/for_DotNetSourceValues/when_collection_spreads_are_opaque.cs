// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_collection_spreads_are_opaque : given.a_compilation
{
    DotNetBounded<DotNetCollectionValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Spreads.cs",
            """
            namespace Values;

            public static class Usage
            {
                static int Helper() => 42;
                static int[] Values() => [1, 2];

                public static int[] Bound(int[] values) => [..values];
                public static int[] BoundScalar(int value) => [..value];
                public static int[] Computed => [..Values()];
                public static int[] NestedComputed => [..new[] { Helper() }];
                public static int[] ExactChild => [..new[] { 1, 2 }];
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax ||
                    (clause.Parent is MethodDeclarationSyntax method && method.Identifier.ValueText.StartsWith("Bound", StringComparison.Ordinal)))
                .Select(clause => DotNetSourceValues.Collection(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_retain_the_generic_unknown_for_a_bound_spread_operand() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.OpaqueSpread, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_retain_the_generic_unknown_for_a_bound_non_spreadable_scalar() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.OpaqueSpread, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_bound_operand_failures_after_their_opaque_spreads() => _results.Take(2).SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["..values", "values", "..value", "value"]);
    [Fact] void should_report_a_computed_spread_operand_after_the_opaque_spread() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.OpaqueSpread, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_spread_before_its_computed_operand() => FailureSources(2).ShouldEqual(["..Values()", "Values()"]);
    [Fact] void should_preserve_intrinsic_nested_failures_without_recovering_values() => FailureSources(3).ShouldEqual(["..new[] { Helper() }", "Helper()"]);
    [Fact] void should_never_recover_values_from_an_exact_spread_operand() => FailureSources(4).ShouldEqual(["..new[] { 1, 2 }"]);
    [Fact] void should_publish_no_partial_collection_for_any_spread() => _results.All(_ => _ is DotNetUnknown<DotNetCollectionValue>).ShouldBeTrue();

    DotNetValueFailureKind[] FailureKinds(int index) => [.. Failures(_results[index]).Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. Failures(_results[index]).Select(_ => SourceText(_.Source))];

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetCollectionValue> result) => ((DotNetUnknown<DotNetCollectionValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
