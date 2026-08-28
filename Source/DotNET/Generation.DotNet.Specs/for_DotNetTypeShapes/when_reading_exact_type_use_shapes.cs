// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_reading_exact_type_use_shapes : given.a_compilation
{
    IReadOnlyDictionary<string, TypeUseDefinition> _types = null!;
    SubjectId _conceptSubject = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Ordering/Shapes.cs",
            """
            #nullable enable
            namespace Ordering;
            public sealed record CustomerCode;
            public sealed record Shapes(
                CustomerCode Value,
                CustomerCode? OptionalValue,
                System.Collections.Generic.IReadOnlyList<CustomerCode> Values,
                System.Collections.Generic.IReadOnlyList<CustomerCode?> OptionalElements,
                System.Collections.Generic.IReadOnlyList<CustomerCode>? OptionalCollection,
                System.Collections.Generic.IReadOnlyList<System.Collections.Generic.IReadOnlyList<CustomerCode?>> NestedValues);
            """));
        var project = new DotNetProjectCompilation
        {
            Name = "Ordering",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        var context = new DotNetAnalysisContext([project]);
        var concept = TypeNamed(compilation, "Ordering.CustomerCode");
        _conceptSubject = project.SubjectForType(concept);
        _types = TypeNamed(compilation, "Ordering.Shapes").GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public)
            .ToDictionary(
                property => property.Name,
                property => DotNetTypeShapes.TypeUseFor(property.Type, context),
                StringComparer.Ordinal);
    }

    [Fact] void should_preserve_a_named_type() => Shape("Value").ShouldEqual("Named");
    [Fact] void should_preserve_an_optional_named_type() => Shape("OptionalValue").ShouldEqual("Optional|Named");
    [Fact] void should_preserve_a_collection() => Shape("Values").ShouldEqual("Collection|Named");
    [Fact] void should_distinguish_optional_collection_elements() => Shape("OptionalElements").ShouldEqual("Collection|Optional|Named");
    [Fact] void should_distinguish_an_optional_collection() => Shape("OptionalCollection").ShouldEqual("Optional|Collection|Named");
    [Fact] void should_preserve_nested_collection_and_element_shape() => Shape("NestedValues").ShouldEqual("Collection|Collection|Optional|Named");
    [Fact] void should_bind_every_terminal_source_type_to_its_exact_subject() => string.Join('|', _types.Where(item => item.Value.ObservedTypeSubject != _conceptSubject).Select(item => $"{item.Key}:{item.Value.Name}:{item.Value.ObservedTypeSubject?.Value}")).ShouldEqual(string.Empty);

    string Shape(string property) => string.Join('|', _types[property].Shape);
}
