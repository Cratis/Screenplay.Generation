// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_conditional_values_are_wrapped : given.a_compilation
{
    DotNetValueFailure[] _failures = null!;
    DotNetValueFailure[] _shortCircuitFailures = null!;
    DotNetBounded<DotNetSourceValue>[] _controls = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/WrappedConditionals.cs",
            """
            namespace Values;

            public static class Usage
            {
                static void Inspect(int value) { }
                static void InspectBool(bool value) { }
                static int Pick(int value) => value;

                public static void Run(bool condition, int? optional)
                {
                    Inspect((int)(condition ? 1 : 2));
                    Inspect((int)(condition switch { true => 1, false => 2 }));
                    Inspect((int)(optional ?? 1));
                    Inspect(Pick(condition ? 1 : 2));
                    InspectBool((bool)(false && condition));
                    InspectBool((bool)(true || condition));
                    InspectBool((bool)(condition && true));
                    InspectBool((bool)(condition || false));
                    InspectBool((false && condition)!);
                    InspectBool(false & true);
                    InspectBool(condition & true);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        _failures =
        [
            .. invocations
                .Where(invocation => invocation.Expression.ToString() == "Inspect")
                .Select(invocation => invocation.ArgumentList.Arguments.Single().Expression)
                .Select(expression => ((DotNetUnknown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Failures.Single())
        ];
        var boolResults = invocations
            .Where(invocation => invocation.Expression.ToString() == "InspectBool")
            .Select(invocation => DotNetSourceValues.Extract(invocation.ArgumentList.Arguments.Single().Expression, semanticModel))
            .ToArray();
        _shortCircuitFailures =
        [
            .. boolResults.Take(5)
                .Select(result => ((DotNetUnknown<DotNetSourceValue>)result).Failures.Single())
        ];
        _controls = [.. boolResults.Skip(5)];
    }

    [Fact] void should_classify_only_the_conditionals_under_transparent_wrappers() => _failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Conditional, DotNetValueFailureKind.Computed]);
    [Fact] void should_locate_wrapped_conditionals_at_the_inner_conditional_syntax() => _failures.Take(3).Select(_ => SourceText(_.Source)).ShouldEqual(["condition ? 1 : 2", "condition switch { true => 1, false => 2 }", "optional ?? 1"]);
    [Fact] void should_not_recursively_classify_a_conditional_inside_a_call() => SourceText(_failures[3].Source).ShouldEqual("Pick(condition ? 1 : 2)");
    [Fact] void should_classify_constant_nonconstant_and_null_suppressed_short_circuit_operators_as_conditional() => _shortCircuitFailures.Select(_ => _.Kind).ShouldEqual(Enumerable.Repeat(DotNetValueFailureKind.Conditional, 5));
    [Fact] void should_locate_each_short_circuit_failure_at_the_inner_binary_expression() => _shortCircuitFailures.Select(_ => SourceText(_.Source)).ShouldEqual(["false && condition", "true || condition", "condition && true", "condition || false", "false && condition"]);
    [Fact] void should_keep_a_constant_non_short_circuit_boolean_expression_known() => _controls[0].ShouldBeOfExactType<DotNetKnown<DotNetSourceValue>>();
    [Fact] void should_keep_a_nonconstant_non_short_circuit_boolean_expression_computed() => ((DotNetUnknown<DotNetSourceValue>)_controls[1]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Computed);
    [Fact] void should_locate_the_nonconstant_non_short_circuit_control_at_the_binary_expression() => SourceText(((DotNetUnknown<DotNetSourceValue>)_controls[1]).Failures.Single().Source).ShouldEqual("condition & true");

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
