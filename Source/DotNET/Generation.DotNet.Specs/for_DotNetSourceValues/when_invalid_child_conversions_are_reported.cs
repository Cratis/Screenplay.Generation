// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_invalid_child_conversions_are_reported : given.a_compilation
{
    DotNetUnknown<DotNetPayloadValue> _payload = null!;
    DotNetUnknown<DotNetCollectionValue> _collection = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InvalidChildConversions.cs",
            """
            namespace Values;

            public sealed class Payload
            {
                public int Value { get; init; }
            }

            public static class Usage
            {
                public static Payload InvalidPayload => new() { Value = "not an int" };
                public static int[] InvalidCollection => new int[] { "not an int" };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Select(_ => _.Expression).ToArray();
        _payload = (DotNetUnknown<DotNetPayloadValue>)DotNetSourceValues.Payload(expressions[0], semanticModel);
        _collection = (DotNetUnknown<DotNetCollectionValue>)DotNetSourceValues.Collection(expressions[1], semanticModel);
    }

    [Fact] void should_report_one_exact_payload_conversion_failure() => _payload.Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_payload_failure_at_the_authored_rhs() => SourceText(_payload.Failures.Single().Source).ShouldEqual("\"not an int\"");
    [Fact] void should_report_one_exact_collection_conversion_failure() => _collection.Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_collection_failure_at_the_authored_element() => SourceText(_collection.Failures.Single().Source).ShouldEqual("\"not an int\"");

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
