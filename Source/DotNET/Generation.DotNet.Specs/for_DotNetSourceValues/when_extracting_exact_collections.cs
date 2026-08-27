// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_exact_collections : given.a_compilation
{
    DotNetCollectionValue[] _collections = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Collections.cs",
            """
            using System.Collections.Generic;

            namespace Values;

            public record Address(string City);

            public static class Usage
            {
                public static int[] Explicit => new int[] { 1, 2 };
                public static int[] Implicit => new[] { 1, 2 };
                public static int[] Expression => [1, 2];
                public static List<int> Initializer => new() { 1, 2 };
                public static Address[] Nested => [new Address("Oslo"), new Address("Bergen")];
                public static IEnumerable<int> ConvertedExplicit => new int[] { 1, 2 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _collections =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Select(_ => _.Expression)
                .Select(expression => ((DotNetKnown<DotNetCollectionValue>)DotNetSourceValues.Collection(expression, semanticModel)).Value)
        ];
    }

    [Fact] void should_extract_every_supported_collection_shape() => _collections.Length.ShouldEqual(6);
    [Fact] void should_preserve_scalar_elements_in_authored_order() => _collections.Take(4).All(collection => collection.Values.Select(_ => ((DotNetConstantValue)_.Value).Value).SequenceEqual([1, 2])).ShouldBeTrue();
    [Fact] void should_preserve_exact_collection_types() => _collections.Take(3).All(collection => collection.Type is IArrayTypeSymbol).ShouldBeTrue();
    [Fact] void should_preserve_an_explicit_array_type_under_interface_conversion() => (_collections[5].Type is IArrayTypeSymbol).ShouldBeTrue();
    [Fact] void should_preserve_nested_payload_elements() => _collections[4].Values.Select(element => ((DotNetConstantValue)((DotNetPayloadValue)element.Value).Values.Single().Value).Value).ShouldEqual(["Oslo", "Bergen"]);
    [Fact] void should_preserve_every_element_source_location() => _collections.All(collection => collection.Values.All(element => element.Source.IsInSource)).ShouldBeTrue();
}
