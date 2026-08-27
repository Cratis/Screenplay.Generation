// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_nested_payload_index_keys_are_not_exact : given.a_compilation
{
    DotNetUnknown<DotNetPayloadValue> _result = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/NestedIndex.cs",
            """
            using System.Collections.Generic;

            namespace Values;

            public sealed class Payload
            {
                public Dictionary<int, int> Values { get; } = [];
            }

            public static class Usage
            {
                static int Helper() => 42;
                public static Payload Value => new() { Values = { [MissingKey] = Helper() } };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Single(clause => clause.Parent is PropertyDeclarationSyntax property &&
                string.Equals(property.Identifier.ValueText, "Value", StringComparison.Ordinal))
            .Expression;
        _result = (DotNetUnknown<DotNetPayloadValue>)DotNetSourceValues.Payload(expression, semanticModel);
    }

    [Fact] void should_report_shape_then_index_key_then_rhs_failures() => _result.Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed]);
    [Fact] void should_preserve_every_nested_index_failure_location() => _result.Failures.Select(_ => SourceText(_.Source)).ShouldEqual(["Values", "MissingKey", "Helper()"]);
    [Fact] void should_publish_no_partial_payload() => _result.ShouldBeOfExactType<DotNetUnknown<DotNetPayloadValue>>();

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
