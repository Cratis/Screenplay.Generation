// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_extracting_exact_payloads : given.a_compilation
{
    DotNetPayloadValue _explicit = null!;
    DotNetPayloadValue _implicit = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Payloads.cs",
            """
            namespace Values;

            public record Address(string City);

            public sealed class Request
            {
                public Request(string name, int count, Address address)
                {
                    Name = name;
                    Count = count;
                    Address = address;
                }

                public string Name { get; }
                public int Count { get; }
                public Address Address { get; }
                public bool Active { get; init; }
            }

            public static class Usage
            {
                public static Request Explicit => new Request(count: 42, address: new Address("Oslo"), name: "Screenplay") { Active = true };
                public static Request Implicit => new("Screenplay", 42, new("Oslo")) { Active = true };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expressions = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Select(_ => _.Expression).ToArray();
        _explicit = ((DotNetKnown<DotNetPayloadValue>)DotNetSourceValues.Payload(expressions[0], semanticModel)).Value;
        _implicit = ((DotNetKnown<DotNetPayloadValue>)DotNetSourceValues.Payload(expressions[1], semanticModel)).Value;
    }

    [Fact] void should_preserve_the_payload_type() => _explicit.Type.Name.ShouldEqual("Request");
    [Fact] void should_order_constructor_values_by_formal_parameter_then_initializers() => _explicit.Values.Select(_ => _.Name).ShouldEqual(["name", "count", "address", "Active"]);
    [Fact] void should_preserve_exact_scalar_values() => _explicit.Values.Take(2).Select(_ => ((DotNetConstantValue)_.Value).Value).ShouldEqual(["Screenplay", 42]);
    [Fact] void should_preserve_nested_payloads() => ((DotNetConstantValue)((DotNetPayloadValue)_explicit.Values[2].Value).Values.Single().Value).Value.ShouldEqual("Oslo");
    [Fact] void should_preserve_initializer_values() => ((DotNetConstantValue)_explicit.Values[3].Value).Value.ShouldEqual(true);
    [Fact] void should_preserve_formal_parameter_and_member_symbols() => _explicit.Values.Select(_ => _.Symbol.Kind).ShouldEqual([SymbolKind.Parameter, SymbolKind.Parameter, SymbolKind.Parameter, SymbolKind.Property]);
    [Fact] void should_preserve_the_same_values_for_target_typed_construction() => Canonical(_implicit).ShouldEqual(Canonical(_explicit));

    static string[] Canonical(DotNetPayloadValue payload) =>
    [
        .. payload.Values.Select(value => $"{value.Name}:{Value(value.Value)}")
    ];

    static string Value(DotNetSourceValue value) => value switch
    {
        DotNetConstantValue constant => constant.Value?.ToString() ?? "null",
        DotNetPayloadValue payload => $"{payload.Type.Name}({string.Join(',', Canonical(payload))})",
        _ => value.GetType().Name
    };
}
