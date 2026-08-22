// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation.DotNet.for_DotNetTypeShapes;

public class when_reading_source_type_references : given.a_compilation
{
    DotNetProjectCompilation _project = null!;
    IReadOnlyList<PropertyDefinition> _properties = null!;
    INamedTypeSymbol _orderId = null!;

    void Because()
    {
        var compilation = CompilationFrom(new SourceFile(
            "/workspace/Banking/Order.cs",
            """
            namespace Banking;
            public readonly record struct OrderId(System.Guid Value);
            public record OrderPlaced(OrderId? OrderId, System.Guid CorrelationId);
            """));
        _project = new DotNetProjectCompilation
        {
            Name = "Banking",
            Compilation = compilation,
            AuthoredSyntaxTrees = compilation.SyntaxTrees.ToHashSet()
        };
        _orderId = TypeNamed(compilation, "Banking.OrderId");
        _properties = DotNetTypeShapes.PropertiesOf(
            TypeNamed(compilation, "Banking.OrderPlaced"),
            new DotNetAnalysisContext([_project]));
    }

    [Fact] void should_bind_the_exact_source_subject() => _properties[0].Type.Subject.ShouldEqual(_project.SubjectForType(_orderId));
    [Fact] void should_keep_the_source_reference_optional() => _properties[0].Type.IsOptional.ShouldBeTrue();
    [Fact] void should_not_assign_a_subject_to_an_external_primitive() => _properties[1].Type.Subject.ShouldBeNull();
}
