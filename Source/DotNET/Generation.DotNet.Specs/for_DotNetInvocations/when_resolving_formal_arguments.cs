// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetInvocations;

public class when_resolving_formal_arguments : given.a_compilation
{
    ArgumentSyntax? _omittedOptional;
    ArgumentSyntax _reorderedNamed = null!;
    ArgumentSyntax _explicitParams = null!;
    ArgumentSyntax? _expandedParams;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Configuration/Arguments.cs",
            """
            namespace Configuration;
            public static class Arguments
            {
                public static void Target(
                    string first,
                    string second = "second",
                    string third = "third",
                    params string[] values)
                {
                }

                public static void Invoke()
                {
                    Target(third: "third", first: "first");
                    Target("first", "second", "third", values: ["one", "two"]);
                    Target("first", "second", "third", "one", "two");
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(_ => _.Expression.ToString() == "Target")
            .ToArray();
        var reordered = invocations[0];
        var explicitParams = invocations[1];
        var expandedParams = invocations[2];
        var reorderedMethod = DotNetInvocations.MethodFor(reordered, semanticModel)!;
        var explicitParamsMethod = DotNetInvocations.MethodFor(explicitParams, semanticModel)!;
        var expandedParamsMethod = DotNetInvocations.MethodFor(expandedParams, semanticModel)!;

        _omittedOptional = DotNetInvocations.ArgumentForParameter(reordered, reorderedMethod, "second", semanticModel);
        _reorderedNamed = DotNetInvocations.ArgumentForParameter(reordered, reorderedMethod, "third", semanticModel)!;
        _explicitParams = DotNetInvocations.ArgumentForParameter(explicitParams, explicitParamsMethod, "values", semanticModel)!;
        _expandedParams = DotNetInvocations.ArgumentForParameter(expandedParams, expandedParamsMethod, "values", semanticModel);
    }

    [Fact] void should_not_map_another_named_argument_to_an_omitted_optional_parameter() => _omittedOptional.ShouldBeNull();
    [Fact] void should_resolve_a_reordered_named_argument() => _reorderedNamed.Expression.ToString().ShouldEqual("\"third\"");
    [Fact] void should_resolve_one_explicit_params_argument() => _explicitParams.Expression.ToString().ShouldEqual("[\"one\", \"two\"]");
    [Fact] void should_not_choose_one_expanded_params_argument() => _expandedParams.ShouldBeNull();
}
