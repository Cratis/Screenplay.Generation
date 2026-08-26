// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_values_are_not_exactly_bounded : given.a_compilation
{
    DotNetValueFailure[] _failures = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Unknown.cs",
            """
            namespace Values;

            public static class Usage
            {
                static void Inspect<T>(T value) { }
                static int Helper() => 42;
                static string Pick(string value) => value;
                static string Pick(System.Uri value) => value.ToString();

                public static void Run(dynamic dynamicValue, bool condition)
                {
                    Inspect(Helper());
                    Inspect(condition ? 1 : 2);
                    Inspect(dynamicValue.Value);
                    Inspect(Missing());
                    Inspect(Pick(default));
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var arguments = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression.ToString().Contains("Inspect", StringComparison.Ordinal))
            .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression)
            .ToArray();
        _failures =
        [
            .. arguments.Select(expression => ((DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Failures.Single())
        ];
    }

    [Fact] void should_classify_each_unknown_shape_deterministically() => _failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Computed, DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Dynamic, DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Ambiguous]);
    [Fact] void should_retain_exact_source_locations() => _failures.Select(_ => (_.Source.GetLineSpan().StartLinePosition.Line + 1, _.Source.GetLineSpan().StartLinePosition.Character + 1)).ShouldEqual([(12, 17), (13, 17), (14, 17), (15, 17), (16, 17)]);
    [Fact] void should_expose_no_partial_values() => _failures.Length.ShouldEqual(5);
}
