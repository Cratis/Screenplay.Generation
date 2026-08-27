// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_initializer_members_cannot_be_assigned : given.a_compilation
{
    DotNetUnknown<DotNetPayloadValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InvalidAssignments.cs",
            """
            namespace Values;

            public sealed class Payload
            {
                public int GetterOnly { get; }
                public readonly int ReadOnly;
                public int PrivateSetter { get; private set; }
                public int Count { get; init; }
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static Payload GetterOnlyConstant => new() { GetterOnly = 42 };
                public static Payload ReadOnlyConstant => new() { ReadOnly = 42 };
                public static Payload InaccessibleSetterConstant => new() { PrivateSetter = 42 };
                public static Payload InvalidConversion => new() { Count = "not an int" };
                public static Payload GetterOnlyComputed => new() { GetterOnly = Helper() };
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

    [Fact] void should_reject_a_getter_only_property_with_one_assignment_shape_failure() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_getter_only_assignment_failure_at_the_member() => FailureSources(0).ShouldEqual(["GetterOnly"]);
    [Fact] void should_reject_a_readonly_field_with_one_assignment_shape_failure() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_readonly_assignment_failure_at_the_member() => FailureSources(1).ShouldEqual(["ReadOnly"]);
    [Fact] void should_reject_an_inaccessible_setter_with_one_assignment_shape_failure() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_inaccessible_setter_failure_at_the_member() => FailureSources(2).ShouldEqual(["PrivateSetter"]);
    [Fact] void should_preserve_only_the_existing_invalid_conversion_failure_for_an_assignable_member() => FailureKinds(3).ShouldEqual([DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_invalid_conversion_failure_at_the_rhs() => FailureSources(3).ShouldEqual(["\"not an int\""]);
    [Fact] void should_order_one_assignment_shape_failure_before_the_intrinsic_rhs_failure() => FailureKinds(4).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_assignment_member_before_the_computed_rhs() => FailureSources(4).ShouldEqual(["GetterOnly", "Helper()"]);
    [Fact] void should_use_one_canonical_assignment_shape_message() => _results.Where((_, index) => index != 3).SelectMany(_ => _.Failures.Where(failure => failure.Kind == DotNetValueFailureKind.Unsupported)).Select(_ => _.Message).Distinct().ShouldEqual(["The payload initializer is not a direct member assignment"]);

    DotNetValueFailureKind[] FailureKinds(int index) => [.. _results[index].Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. _results[index].Failures.Select(_ => _.Source.SourceTree!.GetText().ToString(_.Source.SourceSpan))];
}
