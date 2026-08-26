// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_an_exact_typeof_value : given.a_compilation
{
    DotNetBounded<ITypeSymbol> _result = null!;
    DotNetTypeValue _untyped = null!;

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
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>().Single();
        _result = DotNetSourceValues.TypeOf(expression, semanticModel);
        _untyped = (DotNetTypeValue)((DotNetKnown<DotNetSourceValue>)DotNetSourceValues.Extract(expression, semanticModel)).Value;
    }

    [Fact] void should_resolve_the_exact_aliased_type() => ((DotNetKnown<ITypeSymbol>)_result).Value.ToDisplayString().ShouldEqual("System.Collections.Generic.Dictionary<string, int>");
    [Fact] void should_return_the_same_symbol_through_untyped_extraction() => SymbolEqualityComparer.Default.Equals(((DotNetKnown<ITypeSymbol>)_result).Value, _untyped.Type).ShouldBeTrue();
}
