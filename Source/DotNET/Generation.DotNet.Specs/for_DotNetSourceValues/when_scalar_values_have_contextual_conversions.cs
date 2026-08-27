// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_scalar_values_have_contextual_conversions : given.a_compilation
{
    DotNetBounded<DotNetSourceValue>[] _invalid = null!;
    DotNetBounded<DotNetSourceValue>[] _valid = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/ContextualScalars.cs",
            """
            namespace Values;

            public static class Usage
            {
                static void InspectByte(byte value) { }
                static void InspectInt(int value) { }
                static void InspectObject(object value) { }

                public static void Run()
                {
                    InspectByte(256);
                    InspectInt("not an int");
                    InspectInt(typeof(string));
                    InspectByte(1);
                    InspectObject(42);
                    InspectObject(typeof(string));
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var results = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Where(invocation => invocation.Expression.ToString().StartsWith("Inspect", StringComparison.Ordinal))
            .Select(invocation => DotNetSourceValues.Extract(invocation.ArgumentList.Arguments.Single().Expression, semanticModel))
            .ToArray();
        _invalid = [.. results.Take(3)];
        _valid = [.. results.Skip(3)];
    }

    [Fact] void should_reject_every_invalid_contextual_conversion_once() => _invalid.SelectMany(Failures).Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported, DotNetValueFailureKind.Unsupported]);
    [Fact] void should_locate_each_invalid_conversion_at_the_authored_expression() => _invalid.SelectMany(Failures).Select(_ => SourceText(_.Source)).ShouldEqual(["256", "\"not an int\"", "typeof(string)"]);
    [Fact] void should_preserve_a_valid_contextual_numeric_constant() => ((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_valid[0]).Value).Value.ShouldEqual(1);
    [Fact] void should_preserve_the_contextual_numeric_type() => ((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_valid[0]).Value).Type!.SpecialType.ShouldEqual(SpecialType.System_Byte);
    [Fact] void should_preserve_a_valid_boxed_constant() => ((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_valid[1]).Value).Value.ShouldEqual(42);
    [Fact] void should_preserve_a_typeof_value_converted_to_object() => ((DotNetKnown<DotNetSourceValue>)_valid[2]).Value.ShouldBeOfExactType<DotNetTypeValue>();

    static IReadOnlyList<DotNetValueFailure> Failures(DotNetBounded<DotNetSourceValue> result) => ((DotNetUnknown<DotNetSourceValue>)result).Failures;

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
