// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_an_exact_typeof_value : given.a_compilation
{
    DotNetBounded<ITypeSymbol> _result = null!;
    DotNetTypeValue _untyped = null!;
    DotNetBounded<DotNetSourceValue> _object = null!;
    DotNetBounded<DotNetSourceValue> _dynamic = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Type.cs",
            """
            using Model = System.Collections.Generic.Dictionary<string, int>;

            namespace Values;

            public static class Usage
            {
                public static System.Type Type => typeof(Model);
                public static object Object => typeof(Model);
                public static dynamic Dynamic => typeof(Model);
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().ToArray();
        _result = DotNetSourceValues.TypeOf(expressions[0], semanticModel);
        _untyped = (DotNetTypeValue)((DotNetKnown<DotNetSourceValue>)DotNetSourceValues.Extract(expressions[0], semanticModel)).Value;
        _object = DotNetSourceValues.Extract(expressions[1], semanticModel);
        _dynamic = DotNetSourceValues.Extract(expressions[2], semanticModel);
    }

    [Fact] void should_resolve_the_exact_aliased_type() => ((DotNetKnown<ITypeSymbol>)_result).Value.ToDisplayString().ShouldEqual("System.Collections.Generic.Dictionary<string, int>");
    [Fact] void should_return_the_same_symbol_through_untyped_extraction() => SymbolEqualityComparer.Default.Equals(((DotNetKnown<ITypeSymbol>)_result).Value, _untyped.Type).ShouldBeTrue();
    [Fact] void should_preserve_a_typeof_value_contextually_converted_to_object() => _object.ShouldBeOfExactType<DotNetKnown<DotNetSourceValue>>();
    [Fact] void should_reject_a_typeof_value_contextually_converted_to_dynamic() => ((DotNetUnknown<DotNetSourceValue>)_dynamic).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Dynamic);
    [Fact] void should_locate_the_dynamic_failure_at_the_authored_typeof_expression() => SourceText(((DotNetUnknown<DotNetSourceValue>)_dynamic).Failures.Single().Source).ShouldEqual("typeof(Model)");

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
