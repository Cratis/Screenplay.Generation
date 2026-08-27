// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_constructor_binding_fails : given.a_compilation
{
    DotNetUnknown<DotNetPayloadValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/EarlyUnboundPayloads.cs",
            """
            namespace Values;

            public sealed class Payload
            {
                public Payload(int first, int second) => _ = (first, second);
                public int Count { get; init; }
                public int this[string name, int position]
                {
                    set => _ = (name, position, value);
                }
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static Payload Indexed => new Payload(Helper()) { [MissingKey, Helper()] = Helper() };
                public static Payload Direct => new Payload(Helper()) { Count = Helper() };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => (DotNetUnknown<DotNetPayloadValue>)DotNetSourceValues.Payload(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_report_binding_then_argument_then_index_shape_keys_and_rhs() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_preserve_exact_binding_argument_index_key_and_rhs_locations() => FailureSources(0).ShouldEqual(["new Payload(Helper()) { [MissingKey, Helper()] = Helper() }", "Helper()", "[MissingKey, Helper()]", "MissingKey", "Helper()", "Helper()"]);
    [Fact] void should_keep_a_valid_direct_initializer_shape_when_only_the_constructor_is_unbound() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_not_add_a_member_shape_failure_for_the_valid_direct_initializer() => FailureSources(1).ShouldEqual(["new Payload(Helper()) { Count = Helper() }", "Helper()", "Helper()"]);

    DotNetValueFailureKind[] FailureKinds(int index) => [.. _results[index].Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. _results[index].Failures.Select(_ => SourceText(_.Source))];

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
