// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetSymbols;

public class when_unwrapping_collection_element_types : given.a_compilation
{
    ITypeSymbol _arrayElement = null!;
    ITypeSymbol _enumerableElement = null!;
    ITypeSymbol _customEnumerableElement = null!;
    ITypeSymbol _scalar = null!;
    INamedTypeSymbol _message = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Messaging/Batches.cs",
            """
            namespace Messaging;
            public record Message;
            public sealed class MessageBatch : System.Collections.Generic.List<Message>;
            public record Batches(
                Message[] Array,
                System.Collections.Generic.IEnumerable<Message> Enumerable,
                MessageBatch Custom,
                string Scalar);
            """));
        _message = TypeNamed(compilation, "Messaging.Message");
        var properties = TypeNamed(compilation, "Messaging.Batches").GetMembers().OfType<IPropertySymbol>().ToDictionary(_ => _.Name);

        _arrayElement = DotNetSymbols.ElementTypeOf(properties["Array"].Type);
        _enumerableElement = DotNetSymbols.ElementTypeOf(properties["Enumerable"].Type);
        _customEnumerableElement = DotNetSymbols.ElementTypeOf(properties["Custom"].Type);
        _scalar = DotNetSymbols.ElementTypeOf(properties["Scalar"].Type);
    }

    [Fact] void should_unwrap_an_array() => _arrayElement.ShouldEqual(_message);
    [Fact] void should_unwrap_a_generic_enumerable() => _enumerableElement.ShouldEqual(_message);
    [Fact] void should_unwrap_an_enumerable_shape() => _customEnumerableElement.ShouldEqual(_message);
    [Fact] void should_leave_a_scalar_unchanged() => _scalar.SpecialType.ShouldEqual(SpecialType.System_String);
}
