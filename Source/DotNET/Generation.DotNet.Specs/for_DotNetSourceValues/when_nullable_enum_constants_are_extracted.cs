// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_nullable_enum_constants_are_extracted : given.a_compilation
{
    DotNetBounded<DotNetSourceValue>[] _results = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/NullableEnums.cs",
            """
            namespace Values;

            public enum Unique { None = 0 }
            public enum Aliased { None = 0, Default = 0 }
            public enum Missing { One = 1 }

            public static class Usage
            {
                static void InspectUnique(Unique? value) { }
                static void InspectAliased(Aliased? value) { }
                static void InspectMissing(Missing? value) { }

                public static void Run()
                {
                    InspectUnique(0);
                    InspectAliased(0);
                    InspectMissing(0);
                    InspectUnique(null);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _results =
        [
            .. tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(invocation => invocation.Expression.ToString().StartsWith("Inspect", StringComparison.Ordinal))
                .Select(invocation => DotNetSourceValues.Extract(invocation.ArgumentList.Arguments.Single().Expression, semanticModel))
        ];
    }

    [Fact] void should_preserve_the_unique_nullable_enum_member_symbol() => ((IFieldSymbol)((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_results[0]).Value).Value!).Name.ShouldEqual("None");
    [Fact] void should_reject_an_aliased_nullable_enum_value() => ((DotNetUnknown<DotNetSourceValue>)_results[1]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Ambiguous);
    [Fact] void should_reject_a_nullable_enum_value_without_a_declared_member() => ((DotNetUnknown<DotNetSourceValue>)_results[2]).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Unsupported);
    [Fact] void should_preserve_null_as_an_exact_nullable_enum_constant() => ((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_results[3]).Value).Value.ShouldBeNull();
    [Fact] void should_publish_no_underlying_numeric_nullable_enum_values() => _results.Take(3).OfType<DotNetKnown<DotNetSourceValue>>().All(result => ((DotNetConstantValue)result.Value).Value is IFieldSymbol).ShouldBeTrue();
}
