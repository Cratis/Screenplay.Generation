// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_values_are_not_semantically_valid : given.a_compilation
{
    DotNetBounded<DotNetPayloadValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InvalidPayloads.cs",
            """
            namespace Values;

            public sealed class Request
            {
                public Request() { }
                public Request(int count) => Count = count;
                public int Count { get; init; }
            }

            public sealed class Payload
            {
                public Payload(int first, int second) => _ = (first, second);
                public int Count { get; init; }
            }

            public static class Usage
            {
                static int Helper() => 42;
                static string HelperText() => "not an int";

                public static Request InvalidConstructorArgument => new Request("not an int");
                public static Request InvalidInitializer => new Request { Count = "not an int" };
                public static Request InvalidComputedConstructorArgument => new Request(HelperText());
                public static Request InvalidComputedInitializer => new Request { Count = HelperText() };
                public static Payload UnavailableOverload => new Payload(Helper());
                public static Payload UnavailableOverloadWithInitializer => new Payload(Helper()) { Count = Helper() };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => DotNetSourceValues.Payload(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_publish_no_partial_payloads() => _results.All(_ => _ is DotNetUnknown<DotNetPayloadValue>).ShouldBeTrue();
    [Fact] void should_report_binding_then_the_invalid_literal_conversion_for_an_unbound_constructor() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_unbound_construction_then_the_invalid_authored_literal() => FailureSources(0).ShouldEqual(["new Request(\"not an int\")", "\"not an int\""]);
    [Fact] void should_reject_an_invalid_direct_member_rhs_conversion_at_its_authored_value() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_an_invalid_direct_member_rhs_at_its_authored_value() => FailureSources(1).ShouldEqual(["\"not an int\""]);
    [Fact] void should_report_binding_then_intrinsic_computed_failure_for_an_unbound_constructor() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_unbound_computed_constructor_then_its_authored_argument() => FailureSources(2).ShouldEqual(["new Request(HelperText())", "HelperText()"]);
    [Fact] void should_inspect_an_invalid_computed_initializer_rhs_after_its_conversion_failure() => FailureKinds(3).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_aggregate_an_unavailable_constructor_binding_with_its_authored_argument_failure() => FailureKinds(4).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed]);
    [Fact] void should_aggregate_an_unavailable_constructor_binding_with_every_authored_argument_and_initializer_failure() => FailureKinds(5).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed]);
    [Fact] void should_order_binding_then_argument_then_initializer_failure_locations() => FailureSources(5).ShouldEqual(["new Payload(Helper()) { Count = Helper() }", "Helper()", "Helper()"]);

    DotNetValueFailureKind[] FailureKinds(int index) => [.. ((DotNetUnknown<DotNetPayloadValue>)_results[index]).Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. ((DotNetUnknown<DotNetPayloadValue>)_results[index]).Failures.Select(_ => _.Source.SourceTree!.GetText().ToString(_.Source.SourceSpan))];
}
