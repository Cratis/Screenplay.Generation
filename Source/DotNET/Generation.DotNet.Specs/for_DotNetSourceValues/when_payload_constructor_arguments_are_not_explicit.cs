// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_constructor_arguments_are_not_explicit : given.a_compilation
{
    DotNetBounded<DotNetPayloadValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Arguments.cs",
            """
            namespace Values;

            public record Optional(string Name = "default");
            public record Params(params string[] Values);
            public record OptionalWithComputed(int Value, int Other = 42);
            public record ParamsWithComputed(params int[] Values);

            public static class Usage
            {
                static int Helper()
                {
                    return 42;
                }

                public static Optional OmittedOptional => new();
                public static Params OmittedParams => new();
                public static Params OneExpandedParam => new("one");
                public static Params SeveralExpandedParams => new("one", "two");
                public static Params ExplicitArray => new(new[] { "one" });
                public static OptionalWithComputed OmittedOptionalWithComputedChild => new(Helper());
                public static ParamsWithComputed ExpandedParamsWithComputedChild => new(Helper());
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Select(_ => DotNetSourceValues.Payload(_.Expression, semanticModel))
        ];
    }

    [Fact] void should_reject_omitted_optional_and_params_arguments() => _results.Take(2).All(_ => _ is DotNetUnknown<DotNetPayloadValue>).ShouldBeTrue();
    [Fact] void should_reject_one_or_several_expanded_params_arguments() => _results.Skip(2).Take(2).All(_ => _ is DotNetUnknown<DotNetPayloadValue>).ShouldBeTrue();
    [Fact] void should_accept_one_explicit_authored_array_argument() => _results[4].ShouldBeOfExactType<DotNetKnown<DotNetPayloadValue>>();
    [Fact] void should_preserve_the_explicit_array_as_one_collection_value() => ((DotNetCollectionValue)((DotNetKnown<DotNetPayloadValue>)_results[4]).Value.Values.Single().Value).Values.Length.ShouldEqual(1);
    [Fact] void should_aggregate_an_omitted_optional_shape_with_its_computed_child() => ((DotNetUnknown<DotNetPayloadValue>)_results[5]).Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_aggregate_an_expanded_params_shape_with_its_computed_child() => string.Join(',', ((DotNetUnknown<DotNetPayloadValue>)_results[6]).Failures.Select(_ => _.Kind)).ShouldEqual("Unsupported,Computed");
}
