// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_collection_types_or_conversions_are_not_exact : given.a_compilation
{
    DotNetBounded<DotNetCollectionValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InvalidCollections.cs",
            """
            using System.Collections.Generic;

            namespace Values;

            public static class Usage
            {
                public static Missing[] ErrorRecoveryType => new Missing[] { 1 };
                public static List<Missing>[] ErrorContainingGenericType => new List<Missing>[] { };
                public static int[] InvalidElementConversion => new int[] { "not an int" };
                public static string[] InvalidEmptyArrayRoot => new int[] { };
                public static int InvalidEmptyCollectionExpressionRoot => [];
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Select(_ => DotNetSourceValues.Collection(_.Expression, semanticModel))
        ];
    }

    [Fact] void should_reject_an_error_recovery_array_type() => ((DotNetUnknown<DotNetCollectionValue>)_results[0]).Failures.Select(_ => _.Kind).ShouldContain(DotNetValueFailureKind.Unbound);
    [Fact] void should_reject_an_error_containing_named_generic_array_type() => ((DotNetUnknown<DotNetCollectionValue>)_results[1]).Failures.Select(_ => _.Kind).ShouldContain(DotNetValueFailureKind.Unbound);
    [Fact] void should_report_only_the_invalid_element_conversion_for_an_otherwise_valid_root() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_invalid_element_conversion_at_the_child() => FailureSources(2).ShouldEqual(["\"not an int\""]);
    [Fact] void should_reject_an_incompatible_empty_array_at_the_whole_root() => FailureKinds(3).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_reject_an_incompatible_empty_collection_expression_at_the_whole_root() => FailureKinds(4).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_preserve_the_invalid_empty_root_locations() => _results.Skip(3).SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["new int[] { }", "[]"]);
    [Fact] void should_publish_no_partial_collection_for_any_invalid_root() => _results.Skip(3).All(_ => _ is DotNetUnknown<DotNetCollectionValue>).ShouldBeTrue();

    DotNetValueFailureKind[] FailureKinds(int index) => [.. Failures(_results[index]).Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. Failures(_results[index]).Select(_ => SourceText(_.Source))];

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetCollectionValue> result) => ((DotNetUnknown<DotNetCollectionValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
