// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_index_initializers_are_not_supported : given.a_compilation
{
    DotNetBounded<DotNetPayloadValue> _result = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/IndexInitializer.cs",
            """
            namespace Values;

            public sealed class IndexedPayload
            {
                public int this[string name, int position]
                {
                    set => _ = (name, position, value);
                }
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static IndexedPayload Value => new() { [MissingKey, Helper()] = Helper() };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Single(clause => clause.Parent is PropertyDeclarationSyntax)
            .Expression;
        _result = DotNetSourceValues.Payload(expression, semanticModel);
    }

    [Fact] void should_publish_no_partial_payload() => _result.ShouldBeOfExactType<DotNetUnknown<DotNetPayloadValue>>();
    [Fact] void should_report_shape_then_every_key_then_rhs_failure() => Failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_shape_then_every_key_then_rhs_failure() => Failures.Select(_ => _.Source.SourceTree!.GetText().ToString(_.Source.SourceSpan)).ShouldEqual(["[MissingKey, Helper()]", "MissingKey", "Helper()", "Helper()"]);
    [Fact] void should_not_interpret_the_index_target_as_a_payload_member() => Failures.Count(_ => _.Message.Contains("payload initializer", StringComparison.Ordinal)).ShouldEqual(1);

    IReadOnlyList<DotNetValueFailure> Failures => ((DotNetUnknown<DotNetPayloadValue>)_result).Failures;
}
