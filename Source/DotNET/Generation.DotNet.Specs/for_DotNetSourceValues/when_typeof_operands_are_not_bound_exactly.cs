// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_typeof_operands_are_not_bound_exactly : given.a_compilation
{
    DotNetValueFailure[] _failures = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Types.cs",
            """
            using First;
            using Second;

            namespace First { public class Model; }
            namespace Second { public class Model; }

            namespace Values
            {
                public static class Usage
                {
                    public static System.Type Missing => typeof(MissingType);
                    public static System.Type Ambiguous => typeof(Model);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        _failures =
        [
            .. tree.GetRoot().DescendantNodes().OfType<TypeOfExpressionSyntax>()
                .Select(expression => ((DotNetUnknown<ITypeSymbol>)DotNetSourceValues.TypeOf(expression, semanticModel)).Failures.Single())
        ];
    }

    [Fact] void should_classify_missing_and_ambiguous_types_separately() => _failures.Select(_ => _.Kind).ShouldEqual([DotNetValueFailureKind.Unbound, DotNetValueFailureKind.Ambiguous]);
    [Fact] void should_retain_both_exact_typeof_locations() => _failures.Select(_ => _.Source.GetLineSpan().StartLinePosition.Line + 1).ShouldEqual([11, 12]);
}
