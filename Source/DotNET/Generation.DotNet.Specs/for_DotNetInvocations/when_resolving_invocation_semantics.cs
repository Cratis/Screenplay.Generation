// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetInvocations;

public class when_resolving_invocation_semantics : given.a_compilation
{
    IMethodSymbol _reduced = null!;
    IMethodSymbol _static = null!;
    IMethodSymbol _instance = null!;
    ArgumentSyntax _reducedName = null!;
    ArgumentSyntax _staticName = null!;
    IParameterSymbol _reducedRoot = null!;
    IParameterSymbol _staticRoot = null!;
    IParameterSymbol _instanceRoot = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Configuration/Handler.cs",
            """
            namespace Configuration;
            public sealed class Child
            {
                public void Save() { }
            }
            public sealed class Options
            {
                public Child Child { get; } = new();
            }
            public static class Extensions
            {
                public static void Configure(this Options options, string name) { }
            }
            public static class Handler
            {
                public static void Handle(Options options)
                {
                    options.Configure(name: "reduced");
                    Extensions.Configure(options, "static");
                    options.Child.Save();
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var reducedInvocation = invocations.Single(_ => _.ToString().Contains("reduced", StringComparison.Ordinal));
        var staticInvocation = invocations.Single(_ => _.ToString().Contains("static", StringComparison.Ordinal));
        var instanceInvocation = invocations.Single(_ => _.ToString().EndsWith("Save()", StringComparison.Ordinal));

        _reduced = DotNetInvocations.MethodFor(reducedInvocation, semanticModel)!;
        _static = DotNetInvocations.MethodFor(staticInvocation, semanticModel)!;
        _instance = DotNetInvocations.MethodFor(instanceInvocation, semanticModel)!;
        _reducedName = DotNetInvocations.ArgumentForParameter(reducedInvocation, _reduced, "name", semanticModel)!;
        _staticName = DotNetInvocations.ArgumentForParameter(staticInvocation, _static, "name", semanticModel)!;
        _reducedRoot = DotNetInvocations.ReceiverRootParameter(reducedInvocation, _reduced, semanticModel)!;
        _staticRoot = DotNetInvocations.ReceiverRootParameter(staticInvocation, _static, semanticModel)!;
        _instanceRoot = DotNetInvocations.ReceiverRootParameter(instanceInvocation, _instance, semanticModel)!;
    }

    [Fact] void should_resolve_the_same_extension_definition() => DotNetInvocations.DefinitionOf(_reduced).ShouldEqual(DotNetInvocations.DefinitionOf(_static));
    [Fact] void should_resolve_a_named_reduced_argument() => _reducedName.Expression.ToString().ShouldEqual("\"reduced\"");
    [Fact] void should_resolve_a_positional_static_argument() => _staticName.Expression.ToString().ShouldEqual("\"static\"");
    [Fact] void should_resolve_the_reduced_receiver_parameter() => _reducedRoot.Name.ShouldEqual("options");
    [Fact] void should_resolve_the_static_extension_receiver_parameter() => _staticRoot.Name.ShouldEqual("options");
    [Fact] void should_resolve_an_instance_member_chain_to_its_parameter() => _instanceRoot.Name.ShouldEqual("options");
}
