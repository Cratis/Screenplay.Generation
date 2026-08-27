// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_conditionals_are_inside_checked_expressions : given.a_compilation
{
    DotNetUnknown<DotNetSourceValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/CheckedConditionals.cs",
            """
            namespace Values;

            public sealed record Payload(int Value);

            public static class Usage
            {
                static void Inspect(int value) { }

                public static void Run(bool condition)
                {
                    Inspect(checked(condition ? 1 : 2));
                    Inspect(unchecked(condition ? 1 : 2));
                }

                public static Payload Payload(bool condition) => new(checked(condition ? 1 : 2));
                public static int[] Collection(bool condition) => [unchecked(condition ? 1 : 2)];
                public static int[] Dimension(bool condition) => new int[checked(condition ? 1 : 2)] { 1 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();
        var expressions = root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => string.Equals(invocation.Expression.ToString(), "Inspect", StringComparison.Ordinal))
            .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression)
            .Concat(root.DescendantNodes().OfType<ArrowExpressionClauseSyntax>()
                .Where(clause => clause.Parent is MethodDeclarationSyntax method &&
                    !string.Equals(method.Identifier.ValueText, "Run", StringComparison.Ordinal))
                .Select(clause => clause.Expression))
            .ToArray();
        _results =
        [
            .. expressions.Select(expression => (DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel))
        ];
    }

    [Fact] void should_reject_checked_unchecked_payload_collection_and_dimension_conditionals() => _results.SelectMany(_ => _.Failures).Select(_ => _.Kind).ToArray().ShouldEqual([.. Enumerable.Repeat(DotNetValueFailureKind.Conditional, 5)]);
    [Fact] void should_locate_every_failure_at_the_inner_conditional_expression() => _results.SelectMany(_ => _.Failures).Select(_ => SourceText(_.Source)).ToArray().ShouldEqual([.. Enumerable.Repeat("condition ? 1 : 2", 5)]);
    [Fact] void should_publish_no_partial_values() => _results.Length.ShouldEqual(5);

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
