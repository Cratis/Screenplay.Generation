// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetInvocations;

public class when_resolving_receiver_shapes : given.a_compilation
{
    string? _instanceReceiver;
    string? _staticReceiver;
    string? _castRoot;
    string? _elementRoot;
    string? _conditionalExpressionRoot;
    string? _localRoot;

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
            public static class Helper
            {
                public static void Run() { }
            }
            public static class Handler
            {
                public static void Handle(Options options, object boxed, Options[] items)
                {
                    options.Child.Save();
                    Helper.Run();
                    ((Options)boxed).Child.Save();
                    items[0].Child.Save();
                    var viaConditional = options?.Child;
                    var local = new Options();
                    local.Child.Save();
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();

        string? RootOf(InvocationExpressionSyntax invocation) =>
            DotNetInvocations.ReceiverRootParameter(invocation, DotNetInvocations.MethodFor(invocation, semanticModel), semanticModel)?.Name;

        _instanceReceiver = DotNetInvocations.ReceiverFor(invocations[0], DotNetInvocations.MethodFor(invocations[0], semanticModel), semanticModel)?.ToString();
        _staticReceiver = DotNetInvocations.ReceiverFor(invocations[1], DotNetInvocations.MethodFor(invocations[1], semanticModel), semanticModel)?.ToString();
        _castRoot = RootOf(invocations[2]);
        _elementRoot = RootOf(invocations[3]);
        _localRoot = RootOf(invocations[4]);
        _conditionalExpressionRoot = DotNetInvocations.RootParameter(
            tree.GetRoot().DescendantNodes().OfType<ConditionalAccessExpressionSyntax>().Single(),
            semanticModel)?.Name;
    }

    [Fact] void should_resolve_the_instance_method_receiver() => _instanceReceiver.ShouldEqual("options.Child");
    [Fact] void should_not_resolve_a_receiver_for_a_static_method() => _staticReceiver.ShouldBeNull();
    [Fact] void should_resolve_the_root_through_a_cast() => _castRoot.ShouldEqual("boxed");
    [Fact] void should_resolve_the_root_through_an_element_access() => _elementRoot.ShouldEqual("items");
    [Fact] void should_resolve_the_root_of_a_conditional_access_expression() => _conditionalExpressionRoot.ShouldEqual("options");
    [Fact] void should_not_root_a_local_in_a_parameter() => _localRoot.ShouldBeNull();
}
