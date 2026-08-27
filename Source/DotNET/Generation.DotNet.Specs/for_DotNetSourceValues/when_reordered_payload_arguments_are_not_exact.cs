// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_reordered_payload_arguments_are_not_exact : given.a_compilation
{
    DotNetBounded<DotNetPayloadValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/ReorderedPayloadArguments.cs",
            """
            namespace Values;

            public sealed class Payload
            {
                public Payload(int first, int second) => _ = (first, second);
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static Payload ComputedThenConditional(bool condition) => new(second: Helper(), first: condition ? 1 : 2);
                public static Payload ConditionalThenComputed(bool condition) => new(first: condition ? 1 : 2, second: Helper());
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is MethodDeclarationSyntax { Identifier.ValueText: not "Helper" })
                .Select(clause => DotNetSourceValues.Payload(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_append_reordered_named_argument_failures_in_authored_order() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Computed, DotNetValueFailureKind.Conditional]);
    [Fact] void should_preserve_reordered_named_argument_child_locations_in_authored_order() => FailureSources(0).ShouldEqual(["Helper()", "condition ? 1 : 2"]);
    [Fact] void should_preserve_authored_failure_order_when_it_matches_formal_order() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Computed]);
    [Fact] void should_preserve_each_formal_order_control_location() => FailureSources(1).ShouldEqual(["condition ? 1 : 2", "Helper()"]);
    [Fact] void should_publish_no_partial_payload_for_either_failure_order() => _results.All(_ => _ is DotNetUnknown<DotNetPayloadValue>).ShouldBeTrue();

    DotNetValueFailureKind[] FailureKinds(int index) => [.. Failures(_results[index]).Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. Failures(_results[index]).Select(_ => SourceText(_.Source))];

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetPayloadValue> result) => ((DotNetUnknown<DotNetPayloadValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
