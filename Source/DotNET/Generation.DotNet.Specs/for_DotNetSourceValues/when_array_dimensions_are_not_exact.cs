// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_array_dimensions_are_not_exact : given.a_compilation
{
    DotNetBounded<DotNetCollectionValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Arrays.cs",
            """
            namespace Values;

            public readonly struct Dimension
            {
                public static Dimension Two => default;
                public static implicit operator int(Dimension value) => 2;
            }

            public static class Usage
            {
                static int Size() => 2;
                static int Helper() => 42;
                public static int[] Fixed => new int[2] { 1, 2 };
                public static int[] Mismatched => new int[3] { 1, 2 };
                public static int[] Computed => new int[Size()] { 1, 2 };
                public static int[,] Multidimensional => new int[1, 2] { { 1, 2 } };
                public static int[,] ImplicitMultidimensional => new[,] { { 1, 2 } };
                public static int[][] ExplicitJagged => new int[][] { new[] { 1 } };
                public static int[] MismatchedWithComputedChild => new int[2] { Helper() };
                public static int[] ComputedDimensionWithComputedChild => new int[Size()] { Helper(), 2 };
                public static int[] UnsignedDimension => new int[2U] { 1, 2 };
                public static int[] OneDimensionalNestedInitializer => new int[] { { Helper() } };
                public static int[,] MultidimensionalWithComputedChild => new int[1, 1] { { Helper() } };
                public static int[] NegativeDimension => new int[-1] { };
                public static int[] OverflowingDimension => new int[2147483648U] { };
                public static int[] UserDefinedDimension => new int[Dimension.Two] { 1, 2 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax property &&
                    property.Parent is ClassDeclarationSyntax declaration &&
                    string.Equals(declaration.Identifier.ValueText, "Usage", StringComparison.Ordinal))
                .Select(_ => DotNetSourceValues.Collection(_.Expression, semanticModel))
        ];
    }

    [Fact] void should_accept_a_matching_one_dimensional_constant_size() => _results[0].ShouldBeOfExactType<DotNetKnown<DotNetCollectionValue>>();
    [Fact] void should_reject_a_mismatched_size() => ((DotNetUnknown<DotNetCollectionValue>)_results[1]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_reject_a_computed_size() => ((DotNetUnknown<DotNetCollectionValue>)_results[2]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Computed);
    [Fact] void should_reject_a_multidimensional_array() => ((DotNetUnknown<DotNetCollectionValue>)_results[3]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_reject_an_implicit_multidimensional_array() => ((DotNetUnknown<DotNetCollectionValue>)_results[4]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_accept_an_explicit_one_dimensional_jagged_array() => _results[5].ShouldBeOfExactType<DotNetKnown<DotNetCollectionValue>>();
    [Fact] void should_preserve_the_explicit_jagged_child_as_a_collection() => ((DotNetKnown<DotNetCollectionValue>)_results[5]).Value.Values.Single().Value.ShouldBeOfExactType<DotNetCollectionValue>();
    [Fact] void should_aggregate_a_mismatched_dimension_with_its_computed_child() => FailureKinds(6).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_aggregate_a_computed_dimension_with_its_computed_child() => FailureKinds(7).ShouldEqual([DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_reject_an_unsigned_dimension_that_the_compiler_does_not_bind() => FailureKinds(8).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_unsigned_dimension_compiler_failure_at_the_array() => FailureSources(8).ShouldEqual(["new int[2U] { 1, 2 }"]);
    [Fact] void should_reject_a_nested_initializer_in_a_one_dimensional_array_then_aggregate_its_child() => FailureKinds(9).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_one_dimensional_nested_shape_then_its_child() => FailureSources(9).ShouldEqual(["{ Helper() }", "Helper()"]);
    [Fact] void should_preserve_one_multidimensional_rank_failure_then_aggregate_its_child() => FailureKinds(10).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_not_add_a_nested_shape_failure_for_the_multidimensional_row() => FailureSources(10).ShouldEqual(["int[1, 1]", "Helper()"]);
    [Fact] void should_reject_negative_overflowing_and_user_defined_dimensions_as_computed() => _results.Skip(11).SelectMany(Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_each_invalid_dimension_at_its_authored_expression() => _results.Skip(11).SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["-1", "2147483648U", "Dimension.Two"]);
    [Fact] void should_publish_no_partial_collection_for_any_nested_or_invalid_dimension() => _results.Skip(9).All(_ => _ is DotNetUnknown<DotNetCollectionValue>).ShouldBeTrue();

    DotNetValueFailureKind[] FailureKinds(int index) => [.. Failures(_results[index]).Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. Failures(_results[index]).Select(_ => SourceText(_.Source))];

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetCollectionValue> result) => ((DotNetUnknown<DotNetCollectionValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
