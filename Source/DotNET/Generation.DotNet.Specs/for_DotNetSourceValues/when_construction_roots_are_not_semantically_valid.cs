// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_construction_roots_are_not_semantically_valid : given.a_compilation
{
    DotNetBounded<DotNetSourceValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InvalidRoots.cs",
            """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;

            namespace Values;

            public sealed class RequiredPayload
            {
                public required int Value { get; init; }
                public int Other { get; init; }
            }

            public abstract class AbstractPayload
            {
                public int Value { get; init; }
            }

            public sealed class SatisfiedPayload
            {
                [SetsRequiredMembers]
                public SatisfiedPayload(int value) => Value = value;
                public required int Value { get; init; }
            }

            public sealed class RequiredCollection : List<int>
            {
                public required string Name { get; init; }
            }

            public abstract class AbstractCollection : List<int>;

            public sealed class SatisfiedCollection : List<int>
            {
                [SetsRequiredMembers]
                public SatisfiedCollection() => Name = "set";
                public required string Name { get; init; }
            }

            public static class Usage
            {
                static int Helper() => 42;

                public static RequiredPayload MissingRequired => new() { Other = Helper() };
                public static RequiredPayload MissingRequiredWithInvalidChild => new() { Other = "not an int" };
                public static AbstractPayload AbstractPayload => new() { Value = Helper() };
                public static SatisfiedPayload SatisfiedPayload => new(42);
                public static RequiredCollection MissingRequiredCollection => new() { Helper() };
                public static AbstractCollection AbstractCollection => new() { Helper() };
                public static SatisfiedCollection SatisfiedCollection => new() { 1 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is PropertyDeclarationSyntax)
                .Select(clause => DotNetSourceValues.Extract(clause.Expression, semanticModel))
        ];
    }

    [Fact] void should_report_a_missing_required_payload_root_before_its_intrinsic_child() => FailureKinds(0).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_the_missing_required_payload_root_before_its_child() => FailureSources(0).ShouldEqual(["new() { Other = Helper() }", "Helper()"]);
    [Fact] void should_report_one_invalid_child_conversion_after_the_missing_required_root() => FailureKinds(1).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_the_root_then_the_single_invalid_child_conversion() => FailureSources(1).ShouldEqual(["new() { Other = \"not an int\" }", "\"not an int\""]);
    [Fact] void should_report_an_abstract_payload_root_before_its_intrinsic_child() => FailureKinds(2).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_not_reject_a_sets_required_members_payload() => _results[3].ShouldBeOfExactType<DotNetKnown<DotNetSourceValue>>();
    [Fact] void should_report_a_missing_required_collection_root_before_its_intrinsic_child() => FailureSources(4).ShouldEqual(["new() { Helper() }", "Helper()"]);
    [Fact] void should_report_an_abstract_collection_root_before_its_intrinsic_child() => FailureKinds(5).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Computed]);
    [Fact] void should_not_reject_a_sets_required_members_collection() => _results[6].ShouldBeOfExactType<DotNetKnown<DotNetSourceValue>>();

    DotNetValueFailureKind[] FailureKinds(int index) => [.. ((DotNetUnknown<DotNetSourceValue>)_results[index]).Failures.Select(_ => _.Kind)];

    string[] FailureSources(int index) => [.. ((DotNetUnknown<DotNetSourceValue>)_results[index]).Failures.Select(_ => SourceText(_.Source))];

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
