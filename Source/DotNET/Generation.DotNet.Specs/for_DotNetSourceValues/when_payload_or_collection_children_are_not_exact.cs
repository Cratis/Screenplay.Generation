// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_payload_or_collection_children_are_not_exact : given.a_compilation
{
    DotNetUnknown<DotNetSourceValue>[] _unknown = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/UnknownPayloads.cs",
            """
            namespace Values;

            public record Inner(int Value);
            public record Outer(Inner Inner);
            public sealed class Options
            {
                public bool Enabled { get; init; }
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static Inner ComputedPayload => new(Helper());
                public static Outer NestedComputedPayload => new(new Inner(Helper()));
                public static Options DuplicatePayload => new() { Enabled = true, Enabled = Helper() > 0 };
                public static int[] ComputedCollection => new[] { 1, Helper() };
                public static int[] ConditionalCollection(bool condition) => [1, condition ? 2 : 3];
                public static int[] SpreadCollection(int[] values) => [Helper(), ..values];
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
            .Where(clause => clause.Parent is PropertyDeclarationSyntax or MethodDeclarationSyntax { Identifier.ValueText: not "Helper" })
            .Select(_ => _.Expression)
            .ToArray();
        _unknown =
        [
            .. expressions.Select(expression => (DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel))
        ];
    }

    [Fact] void should_return_only_unknown_results() => _unknown.Length.ShouldEqual(6);
    [Fact] void should_classify_every_nested_failure_without_partial_values() => _unknown.SelectMany(_ => _.Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed, DotNetValueFailureKind.DuplicateMember, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Computed, DotNetValueFailureKind.OpaqueSpread, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_retain_exact_failure_locations() => _unknown.All(unknown => unknown.Failures.All(failure => failure.Source.IsInSource)).ShouldBeTrue();
}
