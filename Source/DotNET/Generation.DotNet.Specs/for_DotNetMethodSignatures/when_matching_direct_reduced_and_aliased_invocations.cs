// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetMethodSignatures;

public class when_matching_direct_reduced_and_aliased_invocations : given.a_compilation
{
    DotNetMethodSignature _signature = null!;
    IMethodSymbol[] _candidates = null!;
    IMethodSymbol? _dynamic;
    IMethodSymbol? _ambiguous;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Framework/Extensions.cs",
            """
            using Alias = Framework.Extensions;

            namespace Framework;

            public static class Extensions
            {
                public static string Map<T>(this int source, ref int count, params T[] values)
                {
                    count++;
                    return values[0]?.ToString() ?? source.ToString();
                }
            }

            public static class Ambiguous
            {
                public static string Pick(string value) => value;
                public static string Pick(System.Uri value) => value.ToString();
            }

            public static class Usage
            {
                public static void Run(dynamic dynamicValue)
                {
                    var count = 0;
                    _ = 42.Map(ref count, "reduced");
                    _ = Extensions.Map<string>(42, ref count, "direct");
                    _ = Alias.Map<string>(42, ref count, "alias");
                    _ = global::Framework.Extensions.Map<string>(42, ref count, "qualified");
                    _ = dynamicValue.Map(ref count, "dynamic");
                    _ = Ambiguous.Pick(default);
                }
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var invocations = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().ToArray();
        _candidates =
        [
            .. invocations
                .Where(invocation => invocation.ToString().Contains("Map", StringComparison.Ordinal) && !invocation.ToString().Contains("dynamicValue", StringComparison.Ordinal))
                .Select(invocation => DotNetInvocations.MethodFor(invocation, semanticModel)!)
        ];
        var definition = compilation.GetTypeByMetadataName("Framework.Extensions")!.GetMembers("Map").OfType<IMethodSymbol>().Single();
        _signature = DotNetMethodSignatures.From(definition);
        _dynamic = DotNetInvocations.MethodFor(invocations.Single(invocation => invocation.ToString().Contains("dynamicValue", StringComparison.Ordinal)), semanticModel);
        _ambiguous = DotNetInvocations.MethodFor(invocations.Single(invocation => invocation.ToString().Contains("Ambiguous", StringComparison.Ordinal)), semanticModel);
    }

    [Fact] void should_find_every_intended_semantically_equivalent_invocation() => _candidates.Length.ShouldEqual(4);
    [Fact] void should_match_every_semantically_equivalent_invocation() => _candidates.All(candidate => DotNetMethodSignatures.Matches(candidate, _signature)).ShouldBeTrue();
    [Fact] void should_include_one_reduced_invocation() => _candidates.Count(candidate => candidate.ReducedFrom is not null).ShouldEqual(1);
    [Fact] void should_include_three_direct_static_invocations() => _candidates.Count(candidate => candidate.ReducedFrom is null).ShouldEqual(3);
    [Fact] void should_normalize_the_extension_receiver() => _signature.Parameters[0].IsExtensionReceiver.ShouldBeTrue();
    [Fact] void should_preserve_generic_arity() => _signature.GenericArity.ShouldEqual(1);
    [Fact] void should_preserve_the_ref_parameter() => _signature.Parameters[1].RefKind.ShouldEqual(RefKind.Ref);
    [Fact] void should_preserve_the_params_parameter() => _signature.Parameters[2].IsParams.ShouldBeTrue();
    [Fact] void should_preserve_the_return_type() => _signature.ReturnType.SpecialType.ShouldEqual(SpecialType.System_String);
    [Fact] void should_not_bind_a_dynamic_invocation() => _dynamic.ShouldBeNull();
    [Fact] void should_not_bind_an_ambiguous_invocation() => _ambiguous.ShouldBeNull();
}
