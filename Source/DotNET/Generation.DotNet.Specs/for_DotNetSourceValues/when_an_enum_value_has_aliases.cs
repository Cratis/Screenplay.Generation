// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_an_enum_value_has_aliases : given.a_compilation
{
    DotNetBounded<DotNetSourceValue> _direct = null!;
    DotNetBounded<DotNetSourceValue> _cast = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Enum.cs",
            """
            namespace Values;

            public enum Status
            {
                Active = 1,
                Enabled = 1
            }

            public static class Usage
            {
                public static Status Direct => Status.Enabled;
                public static Status Cast => (Status)1;
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Select(_ => _.Expression).ToArray();
        _direct = DotNetSourceValues.Extract(expressions[0], semanticModel);
        _cast = DotNetSourceValues.Extract(expressions[1], semanticModel);
    }

    [Fact] void should_preserve_the_directly_authored_member() => ((IFieldSymbol)((DotNetConstantValue)((DotNetKnown<DotNetSourceValue>)_direct).Value).Value!).Name.ShouldEqual("Enabled");
    [Fact] void should_reject_a_value_derived_ambiguous_alias() => ((DotNetUnknown<DotNetSourceValue>)_cast).Failures.Single().Kind.ShouldEqual(DotNetValueFailureKind.Ambiguous);
}
