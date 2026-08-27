// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_semantic_types_are_not_recursively_exact : given.a_compilation
{
    DotNetBounded<ITypeSymbol>[] _typeResults = null!;
    DotNetBounded<DotNetPayloadValue>[] _payloadResults = null!;
    DotNetBounded<DotNetSourceValue> _contextualNull = null!;
    DotNetBounded<DotNetCollectionValue> _collection = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/RecursiveTypes.cs",
            """
            namespace Values;

            public sealed class Envelope<T>;
            public class Container<T>
            {
                public class Nested;
            }

            public static class Usage
            {
                static void Inspect<T>(T value) { }
                static void InspectMissing(Missing[] value) { }

                public static unsafe void Run()
                {
                    Inspect(typeof(Missing[]));
                    Inspect(typeof(Missing*));
                    Inspect(typeof(Container<Missing>.Nested));
                    Inspect(new Envelope<dynamic>());
                    Inspect(new Envelope<Missing[]>());
                    InspectMissing(null);
                }

                public static Envelope<Missing>[] Collection => new Envelope<Missing>[] { };
            }
            """));
        compilation = compilation.WithOptions(compilation.Options.WithAllowUnsafe(true));
        var tree = compilation.SyntaxTrees.Single();
        var root = tree.GetRoot();
        var semanticModel = compilation.GetSemanticModel(tree);
        _typeResults =
        [
            .. root.DescendantNodes().OfType<TypeOfExpressionSyntax>()
                .Select(expression => DotNetSourceValues.TypeOf(expression, semanticModel))
        ];
        _payloadResults =
        [
            .. root.DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Where(expression => expression.Type.ToString().StartsWith("Envelope<", StringComparison.Ordinal) && expression.Parent is ArgumentSyntax)
                .Select(expression => DotNetSourceValues.Payload(expression, semanticModel))
        ];
        var contextualNull = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Single(expression => expression.Expression.ToString() == "InspectMissing")
            .ArgumentList.Arguments.Single().Expression;
        _contextualNull = DotNetSourceValues.Extract(contextualNull, semanticModel);
        var collection = root.DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Single().Expression;
        _collection = DotNetSourceValues.Collection(collection, semanticModel);
    }

    [Fact] void should_return_no_partial_known_values() => (_typeResults.All(_ => _ is DotNetUnknown<ITypeSymbol>) && _payloadResults.All(_ => _ is DotNetUnknown<DotNetPayloadValue>) && _contextualNull is DotNetUnknown<DotNetSourceValue> && _collection is DotNetUnknown<DotNetCollectionValue>).ShouldBeTrue();
    [Fact] void should_reject_error_types_nested_in_arrays_pointers_and_containing_generic_types() => _typeResults.SelectMany(Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Unbound]);
    [Fact] void should_preserve_exact_typeof_failure_locations() => _typeResults.SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["typeof(Missing[])", "typeof(Missing*)", "typeof(Container<Missing>.Nested)"]);
    [Fact] void should_distinguish_nested_dynamic_and_error_payload_types() => _payloadResults.SelectMany(Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Dynamic, DotNetValueFailureKind.Unbound]);
    [Fact] void should_preserve_exact_payload_type_failure_locations() => _payloadResults.SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["new Envelope<dynamic>()", "new Envelope<Missing[]>()"]);
    [Fact] void should_reject_a_contextually_converted_null_with_an_error_element_type() => ((DotNetUnknown<DotNetSourceValue>)_contextualNull).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unbound);
    [Fact] void should_locate_the_contextual_type_failure_at_the_authored_null() => SourceText(((DotNetUnknown<DotNetSourceValue>)_contextualNull).Failures.Single().Source).ShouldEqual("null");
    [Fact] void should_apply_the_same_recursive_type_check_to_collections() => ((DotNetUnknown<DotNetCollectionValue>)_collection).Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unbound]);

    static IReadOnlyList<DotNetValueFailure> Failures<T>(DotNetBounded<T> result) => ((DotNetUnknown<T>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
