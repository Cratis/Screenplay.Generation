// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>Returns the exact observation and a separately attributable one-layer printable view.</summary>
public sealed record DotNetTypeUseCompatibilityResult
{
    /// <summary>Gets the exact type-use observation, including every source node.</summary>
    public required TypeUseDefinition Observation { get; init; }

    /// <summary>Gets the explicit version 1 compatibility projection.</summary>
    public required TypeUseCompatibilityProjectionDefinition Projection { get; init; }
}

/// <summary>Builds explicit one-layer compatibility views without changing the existing type-shape helpers.</summary>
public static class DotNetTypeUseCompatibility
{
    /// <summary>Observes an exact Roslyn type in one traversal and selects its one-layer printable symbol.</summary>
    /// <param name="type">The exact source type symbol.</param>
    /// <param name="context">The analyzed authored source context.</param>
    /// <param name="observationId">The identifier of the original type-use fact.</param>
    /// <returns>The lossless observation and the explicit printable projection.</returns>
    public static DotNetTypeUseCompatibilityResult Project(ITypeSymbol type, DotNetAnalysisContext context, FactId observationId)
    {
        var exact = Observe(type, context);
        var optional = exact.Shape[0] == TypeUseShapeKind.Optional;
        var index = optional ? 1 : 0;
        var collection = exact.Shape[index] == TypeUseShapeKind.Collection;
        index += collection ? 1 : 0;
        var selected = exact.Nodes![index];
        return new()
        {
            Observation = exact,
            Projection = new()
            {
                TypeUse = observationId,
                Profile = TypeUseCompatibilityProfile.OneLayerCollection,
                Version = 1,
                SourceNodeIndex = index,
                Type = new()
                {
                    Name = selected.Name,
                    ObservedTypeSubject = selected.Subject,
                    Shape = [.. optional ? new[] { TypeUseShapeKind.Optional } : [], .. collection ? new[] { TypeUseShapeKind.Collection } : [], TypeUseShapeKind.Named]
                }
            }
        };
    }

    static TypeUseDefinition Observe(ITypeSymbol type, DotNetAnalysisContext context)
    {
        var shape = new List<TypeUseShapeKind>();
        var nodes = new List<TypeUseNodeObservation>();
        var current = type;
        while (true)
        {
            nodes.Add(NodeFor(current, context));
            var (kind, next) = Next(current);
            shape.Add(kind);
            if (next is null)
            {
                return new() { Name = nodes[^1].Name, ObservedTypeSubject = nodes[^1].Subject, Shape = shape.ToArray(), Nodes = nodes.ToArray() };
            }

            current = next;
        }
    }

    static (TypeUseShapeKind Kind, ITypeSymbol? Next) Next(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            return (TypeUseShapeKind.Optional, nullable.TypeArguments[0]);
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return (TypeUseShapeKind.Optional, type.WithNullableAnnotation(NullableAnnotation.NotAnnotated));
        }

        var (element, collection) = DotNetSymbols.CollectionElementOf(type);
        return collection ? (TypeUseShapeKind.Collection, element) : (TypeUseShapeKind.Named, null);
    }

    static TypeUseNodeObservation NodeFor(ITypeSymbol type, DotNetAnalysisContext context) => new()
    {
        Name = type is IArrayTypeSymbol ? type.ToDisplayString() : DotNetTypeShapes.TypeName(type),
        Subject = type is INamedTypeSymbol named ? context.SubjectForType(named) : null
    };
}
