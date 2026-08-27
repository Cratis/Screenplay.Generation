// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_initializers_are_not_direct_members : given.a_compilation
{
    DotNetBounded<DotNetSourceValue> _casing = null!;
    DotNetBounded<DotNetSourceValue> _index = null!;
    DotNetBounded<DotNetSourceValue> _nested = null!;
    DotNetBounded<DotNetSourceValue> _empty = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Initializers.cs",
            """
            using System.Collections.Generic;

            namespace Values;

            public sealed class Casing
            {
                public Casing(string value) => _ = value;
                public string Value { get; init; } = string.Empty;
            }

            public sealed class Options
            {
                public List<int> Values { get; } = [];
            }

            public static class Usage
            {
                public static Casing DistinctCasing => new("constructor") { Value = "initializer" };
                public static Dictionary<string, int> Indexed => new() { ["a"] = 1 };
                public static Options NestedCollectionInitializer => new() { Values = { 1, 2 } };
                public static Options EmptyObjectInitializer => new() { };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Where(clause => clause.Parent is PropertyDeclarationSyntax)
            .Select(_ => _.Expression)
            .ToArray();
        _casing = DotNetSourceValues.Extract(expressions[0], semanticModel);
        _index = DotNetSourceValues.Extract(expressions[1], semanticModel);
        _nested = DotNetSourceValues.Extract(expressions[2], semanticModel);
        _empty = DotNetSourceValues.Extract(expressions[3], semanticModel);
    }

    [Fact] void should_preserve_case_distinct_legal_symbols() => ((DotNetPayloadValue)((DotNetKnown<DotNetSourceValue>)_casing).Value).Values.Select(_ => _.Name).ShouldEqual(["value", "Value"]);
    [Fact] void should_reject_an_index_initializer_without_losing_its_key() => ((DotNetUnknown<DotNetSourceValue>)_index).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_reject_an_indirect_nested_collection_initializer() => ((DotNetUnknown<DotNetSourceValue>)_nested).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_classify_an_empty_object_initializer_as_a_payload() => ((DotNetKnown<DotNetSourceValue>)_empty).Value.ShouldBeOfExactType<DotNetPayloadValue>();
}
