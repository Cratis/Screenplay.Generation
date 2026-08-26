// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_additional_value_shapes_are_not_bounded : given.a_compilation
{
    DotNetValueFailureKind[] _kinds = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Shapes.cs",
            """
            namespace Values;

            public static class Usage
            {
                static void Inspect<T>(T value) { }

                public static void Run(dynamic dynamicValue, bool condition)
                {
                    Inspect(dynamicValue?.Value);
                    Inspect(condition switch { true => 1, false => 2 });
                    Inspect(new object());
                    Inspect(MissingValue);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _kinds =
        [
            .. tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression.ToString() == "Inspect")
                .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression)
                .Select(expression => ((DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Failures.Single().Kind)
        ];
    }

    [Fact] void should_classify_every_shape_without_exposing_partial_values() => _kinds.ShouldEqual([DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Computed, DotNetValueFailureKind.Unbound]);
}
