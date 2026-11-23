// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_projecting_scoped_source_nodes : given.a_compilation
{
    static readonly BackendScopeKey _containerScope = new() { Store = new() { Value = "dotnet://Ordering/ContainerStore" } };
    static readonly BackendScopeKey _valueScope = new() { Store = new() { Value = "dotnet://Ordering/ValueStore" } };
    DotNetTypeUseCompatibilityResult _scoped = null!;
    DotNetTypeUseCompatibilityResult _default = null!;
    IReadOnlyList<INamedTypeSymbol> _lookups = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile("/workspace/Ordering/ScopedShapes.cs", """
            namespace Ordering;
            public sealed record Value;
            public sealed class Container<T> : System.Collections.Generic.List<T> { }
            public sealed record Shape(System.Collections.Generic.List<Container<Value>> Values);
            """));
        var project = new DotNetProjectCompilation { Name = "Ordering", Compilation = compilation, AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet() };
        var context = new DotNetAnalysisContext([project]);
        var type = TypeNamed(compilation, "Ordering.Shape").GetMembers("Values").OfType<IPropertySymbol>().Single().Type;
        var container = TypeNamed(compilation, "Ordering.Container`1");
        var lookups = new List<INamedTypeSymbol>();
        _scoped = DotNetTypeUseCompatibility.Project(type, context, new() { Value = "scoped-observation" }, scopeForType: named =>
        {
            lookups.Add(named);
            return SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, container) ? _containerScope : _valueScope;
        });
        _default = DotNetTypeUseCompatibility.Project(type, context, new() { Value = "default-observation" });
        _lookups = lookups;
    }

    [Fact] void should_scope_the_selected_printable_symbol() => _scoped.Projection.Type.ObservedScope.ShouldEqual(_containerScope);
    [Fact] void should_scope_the_selected_source_node_consistently() => _scoped.Observation.Nodes![_scoped.Projection.SourceNodeIndex].Scope.ShouldEqual(_containerScope);
    [Fact] void should_preserve_the_distinct_terminal_scope() => _scoped.Observation.ObservedScope.ShouldEqual(_valueScope);
    [Fact] void should_scope_the_terminal_source_node_consistently() => _scoped.Observation.Nodes![^1].Scope.ShouldEqual(_valueScope);
    [Fact] void should_produce_a_valid_scoped_projection() => TypeUseCompatibilityProjectionValidation.Valid(_scoped.Projection, _scoped.Observation).ShouldBeTrue();
    [Fact] void should_not_assign_a_scope_to_unowned_wrapper_symbols() => _scoped.Observation.Nodes![0].Scope.ShouldBeNull();
    [Fact] void should_only_request_scopes_for_authored_nodes() => _lookups.Count.ShouldEqual(2);
    [Fact] void should_keep_default_observations_unscoped() => _default.Observation.ObservedScope.ShouldBeNull();
    [Fact] void should_keep_default_printable_views_unscoped() => _default.Projection.Type.ObservedScope.ShouldBeNull();
    [Fact] void should_only_request_scopes_for_owned_source_types() => _lookups.Count.ShouldEqual(2);
    [Fact] void should_keep_existing_calls_unscoped() => _default.Observation.Nodes!.All(node => node.Scope is null).ShouldBeTrue();
}
