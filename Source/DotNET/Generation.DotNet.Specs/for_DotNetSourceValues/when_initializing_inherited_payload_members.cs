// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSourceValues;

public class when_initializing_inherited_payload_members : given.a_compilation
{
    DotNetPayloadValue _payload = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/InheritedMembers.cs",
            """
            namespace Values;

            public class BasePayload
            {
                public int BaseProperty { get; init; }
                public int BaseField;
            }

            public sealed class Payload(string name) : BasePayload
            {
                public string Name { get; } = name;
                public int OwnProperty { get; init; }
            }

            public static class Usage
            {
                public static Payload Value => new("payload") { BaseProperty = 1, BaseField = 2, OwnProperty = 3 };
            }
            """));
        var tree = compilation.SyntaxTrees.Single();
        var semanticModel = compilation.GetSemanticModel(tree);
        var expression = tree.GetRoot().DescendantNodes().OfType<ArrowExpressionClauseSyntax>().Single().Expression;
        _payload = ((DotNetKnown<DotNetPayloadValue>)DotNetSourceValues.Payload(expression, semanticModel)).Value;
    }

    [Fact] void should_order_the_constructor_then_inherited_and_own_members_by_authored_order() => _payload.Values.Select(_ => _.Name).ShouldEqual(["name", "BaseProperty", "BaseField", "OwnProperty"]);
    [Fact] void should_preserve_the_inherited_member_declaring_types() => _payload.Values.Skip(1).Select(_ => _.Symbol.ContainingType.Name).ShouldEqual(["BasePayload", "BasePayload", "Payload"]);
    [Fact] void should_preserve_each_authored_value_location() => _payload.Values.Select(_ => SourceText(_.Source)).ShouldEqual(["\"payload\"", "1", "2", "3"]);
    [Fact] void should_preserve_property_and_field_symbol_provenance() => _payload.Values.Skip(1).Select(_ => _.Symbol.Kind).ShouldEqual([SymbolKind.Property, SymbolKind.Field, SymbolKind.Property]);

    static string SourceText(Location source) => source.SourceTree!.GetText().ToString(source.SourceSpan);
}
