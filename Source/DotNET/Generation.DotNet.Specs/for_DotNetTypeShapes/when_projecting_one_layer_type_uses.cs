// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_projecting_one_layer_type_uses : given.a_compilation
{
    IReadOnlyDictionary<string, DotNetTypeUseCompatibilityResult> _results = null!;
    IReadOnlyDictionary<string, TypeReferenceDefinition> _legacy = null!;
    IReadOnlyDictionary<string, TypeUseDefinition> _exact = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/workspace/Ordering/Shapes.cs", """
            #nullable enable
            namespace Ordering;
            public sealed record Value;
            public sealed class Container<T> : System.Collections.Generic.List<T> { }
            public sealed record Shapes(
                string Named,
                string? Optional,
                int? NullableNamed,
                System.Collections.Generic.List<string> Collection,
                System.Collections.Generic.List<string>? OptionalCollection,
                System.Collections.Generic.List<string?> OptionalElements,
                System.Collections.Generic.List<string?>? BothOptional,
                System.Collections.Generic.List<int?> NullableElements,
                System.Collections.Generic.List<System.Collections.Generic.List<string>> Nested,
                System.Collections.Generic.List<System.Collections.Generic.List<Value>> NestedValue,
                System.Collections.Generic.List<Container<Value>> AuthoredContainer);
            """));
        var project = new DotNetProjectCompilation { Name = "Ordering", Compilation = compilation, AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet() };
        var context = new DotNetAnalysisContext([project]);
        var properties = TypeNamed(compilation, "Ordering.Shapes").GetMembers().OfType<IPropertySymbol>()
            .Where(property => !property.IsStatic && property.DeclaredAccessibility == Accessibility.Public).ToArray();
        _results = properties.ToDictionary(property => property.Name, property => DotNetTypeUseCompatibility.Project(property.Type, context, new() { Value = property.Name }), StringComparer.Ordinal);
        _legacy = properties.ToDictionary(property => property.Name, property => DotNetTypeShapes.TypeReferenceFor(property.Type, context), StringComparer.Ordinal);
        _exact = properties.ToDictionary(property => property.Name, property => DotNetTypeShapes.TypeUseFor(property.Type, context), StringComparer.Ordinal);
    }

    [Fact] void should_preserve_named_parity() => Parity("Named").ShouldBeTrue();
    [Fact] void should_preserve_optional_parity() => Parity("Optional").ShouldBeTrue();
    [Fact] void should_preserve_nullable_named_parity() => Parity("NullableNamed").ShouldBeTrue();
    [Fact] void should_preserve_collection_parity() => Parity("Collection").ShouldBeTrue();
    [Fact] void should_preserve_optional_collection_parity() => Parity("OptionalCollection").ShouldBeTrue();
    [Fact] void should_preserve_optional_element_parity() => Parity("OptionalElements").ShouldBeTrue();
    [Fact] void should_preserve_both_optional_parity() => Parity("BothOptional").ShouldBeTrue();
    [Fact] void should_preserve_nullable_value_element_parity() => Parity("NullableElements").ShouldBeTrue();
    [Fact] void should_preserve_nested_collection_parity() => Parity("Nested").ShouldBeTrue();
    [Fact] void should_preserve_authored_container_parity() => Parity("AuthoredContainer").ShouldBeTrue();
    [Fact] void should_select_the_authored_container_identity() => _results["AuthoredContainer"].Projection.Type.ObservedTypeSubject!.Value.ShouldContain("Container");
    [Fact] void should_keep_the_authored_container_terminal() => _results["AuthoredContainer"].Observation.ObservedTypeSubject!.Value.ShouldContain("Value");
    [Fact] void should_keep_existing_exact_observations_unchanged() => _results.All(pair =>
        pair.Value.Observation.Name == _exact[pair.Key].Name && pair.Value.Observation.ObservedTypeSubject == _exact[pair.Key].ObservedTypeSubject &&
        pair.Value.Observation.ObservedScope == _exact[pair.Key].ObservedScope && pair.Value.Observation.Shape.SequenceEqual(_exact[pair.Key].Shape)).ShouldBeTrue();
    [Fact] void should_not_flatten_nullable_value_elements() => _results["NullableElements"].Projection.Type.Name.ShouldEqual("Nullable");
    [Fact] void should_not_flatten_nested_collections() => _results["Nested"].Projection.Type.Name.ShouldEqual("List");
    [Fact] void should_keep_the_hidden_terminal_identity() => _results["NestedValue"].Observation.ObservedTypeSubject!.Value.ShouldContain("Value");
    [Fact] void should_not_project_the_hidden_terminal_identity() => _results["NestedValue"].Projection.Type.ObservedTypeSubject.ShouldBeNull();
    [Fact] void should_keep_exact_wrapper_order() => string.Join('|', _results["BothOptional"].Observation.Shape).ShouldEqual("Optional|Collection|Optional|Named");
    [Fact] void should_keep_all_node_identities() => _results.Values.All(result => result.Observation.Nodes!.Count == result.Observation.Shape.Count).ShouldBeTrue();
    [Fact] void should_reference_each_original_observation() => _results.All(pair => pair.Value.Projection.TypeUse.Value == pair.Key).ShouldBeTrue();

    bool Parity(string name)
    {
        var type = _results[name].Projection.Type;
        var legacy = _legacy[name];
        return type.Name == legacy.Name && type.ObservedTypeSubject == legacy.Subject &&
            type.Shape.Contains(TypeUseShapeKind.Collection) == legacy.IsCollection &&
            type.Shape.Contains(TypeUseShapeKind.Optional) == legacy.IsOptional;
    }
}
