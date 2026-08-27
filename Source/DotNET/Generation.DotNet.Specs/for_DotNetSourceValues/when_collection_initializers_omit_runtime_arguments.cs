// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_collection_initializers_omit_runtime_arguments : given.a_compilation
{
    DotNetCollectionValue[] _collections = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/OmittedRuntimeArguments.cs",
            """
            using System.Collections;
            using System.Collections.Generic;

            namespace Values;

            public sealed class OptionalCollection(int capacity = 4) : IEnumerable<int>
            {
                public void Add(int value, string category = "default") => _ = (value, category, capacity);
                public IEnumerator<int> GetEnumerator() => throw null!;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public sealed class ParamsCollection(params string[] names) : IEnumerable<int>
            {
                public void Add(int value, params string[] tags) => _ = (value, tags, names);
                public IEnumerator<int> GetEnumerator() => throw null!;
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public static class Usage
            {
                public static OptionalCollection Optional => new() { 1 };
                public static ParamsCollection Params => new() { 2 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _collections =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => ((DotNetKnown<DotNetCollectionValue>)DotNetSourceValues.Collection(clause.Expression, semanticModel)).Value)
        ];
    }

    [Fact] void should_allow_omitted_optional_constructor_and_add_arguments() => ScalarValue(0).ShouldEqual(1);
    [Fact] void should_allow_omitted_params_constructor_and_add_arguments() => ScalarValue(1).ShouldEqual(2);
    [Fact] void should_publish_only_the_one_authored_add_argument_for_each_entry() => _collections.All(_ => _.Values.Length == 1).ShouldBeTrue();
    [Fact] void should_preserve_the_exact_authored_add_argument_locations() => _collections.SelectMany(_ => _.Values).Select(_ => SourceText(_.Source)).ShouldEqual(["1", "2"]);

    object? ScalarValue(int index) => ((DotNetConstantValue)_collections[index].Values.Single().Value).Value;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
