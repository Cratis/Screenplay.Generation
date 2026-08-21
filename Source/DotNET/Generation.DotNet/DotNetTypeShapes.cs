// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Converts Roslyn types and public source properties into framework-neutral generation shapes.
/// </summary>
public static class DotNetTypeShapes
{
    /// <summary>
    /// Gets the Screenplay-oriented type reference for a Roslyn type.
    /// </summary>
    /// <param name="type">The Roslyn type.</param>
    /// <returns>The type reference.</returns>
    public static TypeReferenceDefinition TypeReferenceFor(ITypeSymbol type)
    {
        var isOptional = type.NullableAnnotation == NullableAnnotation.Annotated;
        var current = type;
        if (current is INamedTypeSymbol nullable && nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            current = nullable.TypeArguments[0];
            isOptional = true;
        }

        var (elementType, isCollection) = CollectionElementOf(current);
        return new()
        {
            Name = TypeName(elementType),
            IsCollection = isCollection,
            IsOptional = isOptional
        };
    }

    /// <summary>
    /// Gets the public readable instance properties of a source type in declaration order.
    /// </summary>
    /// <param name="type">The source type.</param>
    /// <returns>The property definitions.</returns>
    public static IReadOnlyList<PropertyDefinition> PropertiesOf(INamedTypeSymbol type) =>
    [
        .. type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(_ => !_.IsStatic && !_.IsIndexer && _.DeclaredAccessibility == Accessibility.Public && _.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(SourceOrder)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
            .Select(_ => new PropertyDefinition
            {
                Name = PropertyName(_.Name),
                Type = TypeReferenceFor(_.Type)
            })
    ];

    static (ITypeSymbol Type, bool IsCollection) CollectionElementOf(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol array)
        {
            return (array.ElementType, true);
        }

        if (type.SpecialType == SpecialType.System_String || type is not INamedTypeSymbol named)
        {
            return (type, false);
        }

        var enumerable = named.AllInterfaces
            .Concat([named])
            .FirstOrDefault(_ =>
                _.IsGenericType &&
                _.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == "global::System.Collections.Generic.IEnumerable<T>");

        return enumerable is null ? (type, false) : (enumerable.TypeArguments[0], true);
    }

    static string TypeName(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_String => "String",
        SpecialType.System_Boolean => "Bool",
        SpecialType.System_Byte or
        SpecialType.System_SByte or
        SpecialType.System_Int16 or
        SpecialType.System_UInt16 or
        SpecialType.System_Int32 or
        SpecialType.System_UInt32 or
        SpecialType.System_Int64 or
        SpecialType.System_UInt64 => "Int",
        SpecialType.System_Decimal or
        SpecialType.System_Double or
        SpecialType.System_Single => "Decimal",
        _ => NamedTypeName(type)
    };

    static string NamedTypeName(ITypeSymbol type)
    {
        var metadataName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return metadataName switch
        {
            "global::System.Guid" => "Uuid",
            "global::System.DateOnly" => "Date",
            "global::System.DateTime" or "global::System.DateTimeOffset" => "DateTime",
            _ => type.Name
        };
    }

    static int SourceOrder(IPropertySymbol property) => property.Locations
        .Where(_ => _.IsInSource)
        .Select(_ => _.SourceSpan.Start)
        .DefaultIfEmpty(int.MaxValue)
        .Min();

    static string PropertyName(string name) => name.Length == 0
        ? name
        : $"{char.ToLowerInvariant(name[0])}{name[1..]}";
}

/// <summary>
/// Provides metadata-name-based Roslyn symbol matching without loading framework assemblies.
/// </summary>
public static class DotNetSymbols
{
    /// <summary>
    /// Gets whether a symbol carries an attribute with the specified metadata name.
    /// </summary>
    /// <param name="symbol">The attributed symbol.</param>
    /// <param name="metadataName">The fully qualified attribute metadata name.</param>
    /// <returns><see langword="true"/> when the attribute is present; otherwise, <see langword="false"/>.</returns>
    public static bool HasAttribute(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(_ => _.AttributeClass is not null && DotNetSubjectIds.MetadataName(_.AttributeClass) == metadataName);

    /// <summary>
    /// Gets whether a symbol carries an attribute whose type is or derives from the specified metadata name.
    /// </summary>
    /// <param name="symbol">The attributed symbol.</param>
    /// <param name="metadataName">The fully qualified base attribute metadata name.</param>
    /// <returns><see langword="true"/> when an assignable attribute is present; otherwise, <see langword="false"/>.</returns>
    public static bool HasAttributeAssignableTo(ISymbol symbol, string metadataName) =>
        symbol.GetAttributes().Any(_ => _.AttributeClass is not null && IsOrInheritsFrom(_.AttributeClass, metadataName));

    /// <summary>
    /// Gets whether a type is or inherits from a type with the specified metadata name.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="metadataName">The fully qualified metadata name.</param>
    /// <returns><see langword="true"/> when the type matches; otherwise, <see langword="false"/>.</returns>
    public static bool IsOrInheritsFrom(INamedTypeSymbol type, string metadataName)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (DotNetSubjectIds.MetadataName(current) == metadataName)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets whether a type implements an interface with the specified metadata name.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <param name="metadataName">The fully qualified interface metadata name.</param>
    /// <returns><see langword="true"/> when the interface is implemented; otherwise, <see langword="false"/>.</returns>
    public static bool Implements(INamedTypeSymbol type, string metadataName) =>
        type.AllInterfaces.Any(_ => DotNetSubjectIds.MetadataName(_) == metadataName);
}
