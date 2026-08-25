// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetInvocations;

public class when_resolving_conditional_receivers : given.a_compilation
{
    ExpressionSyntax _conditionalReceiver = null!;
    IParameterSymbol _conditionalRoot = null!;
    IParameterSymbol _conditionalChainRoot = null!;
    ExpressionSyntax? _unqualifiedReceiver;

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
                public void Save() { }
            }
            public sealed class Handler
            {
                public void Handle(Options? options)
                {
                    options?.Save();
                    options?.Child.Save();
                    Save();
                }

                void Save() { }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        var conditional = invocations[0];
        var conditionalChain = invocations[1];
        var unqualified = invocations[2];
        var conditionalMethod = DotNetInvocations.MethodFor(conditional, semanticModel)!;
        var conditionalChainMethod = DotNetInvocations.MethodFor(conditionalChain, semanticModel)!;
        var unqualifiedMethod = DotNetInvocations.MethodFor(unqualified, semanticModel)!;

        _conditionalReceiver = DotNetInvocations.ReceiverFor(conditional, conditionalMethod, semanticModel)!;
        _conditionalRoot = DotNetInvocations.ReceiverRootParameter(conditional, conditionalMethod, semanticModel)!;
        _conditionalChainRoot = DotNetInvocations.ReceiverRootParameter(conditionalChain, conditionalChainMethod, semanticModel)!;
        _unqualifiedReceiver = DotNetInvocations.ReceiverFor(unqualified, unqualifiedMethod, semanticModel);
    }

    [Fact] void should_resolve_the_conditional_receiver() => _conditionalReceiver.ToString().ShouldEqual("options");
    [Fact] void should_resolve_the_conditional_receiver_parameter() => _conditionalRoot.Name.ShouldEqual("options");
    [Fact] void should_resolve_a_conditional_member_chain_to_its_parameter() => _conditionalChainRoot.Name.ShouldEqual("options");
    [Fact] void should_not_invent_an_unqualified_instance_receiver() => _unqualifiedReceiver.ShouldBeNull();
}
