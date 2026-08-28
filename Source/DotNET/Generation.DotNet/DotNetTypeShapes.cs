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
    public static TypeReferenceDefinition TypeReferenceFor(ITypeSymbol type) => CreateTypeReference(type, null);

    /// <summary>
    /// Gets the Screenplay-oriented type reference with an exact source subject when the analyzed projects own it.
    /// </summary>
    /// <param name="type">The Roslyn type.</param>
    /// <param name="context">The analyzed project context used to resolve source subjects.</param>
    /// <returns>The type reference.</returns>
    public static TypeReferenceDefinition TypeReferenceFor(ITypeSymbol type, DotNetAnalysisContext context) =>
        CreateTypeReference(type, context.SubjectForType);

    /// <summary>
    /// Gets the exact optionality and collection shape of a Roslyn type use.
    /// </summary>
    /// <param name="type">The Roslyn type at the use site.</param>
    /// <param name="context">The analyzed project context used to resolve the terminal source subject.</param>
    /// <returns>The exact source-neutral type use from outermost wrapper to terminal named type.</returns>
    public static TypeUseDefinition TypeUseFor(ITypeSymbol type, DotNetAnalysisContext context)
    {
        var shape = new List<TypeUseShapeKind>();
        var current = type;
        while (true)
        {
            if (current is INamedTypeSymbol nullable &&
                nullable.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                shape.Add(TypeUseShapeKind.Optional);
                current = nullable.TypeArguments[0];
                continue;
            }

            if (current.NullableAnnotation == NullableAnnotation.Annotated)
            {
                shape.Add(TypeUseShapeKind.Optional);
                current = current.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                continue;
            }

            var (elementType, isCollection) = CollectionElementOf(current);
            if (isCollection)
            {
                shape.Add(TypeUseShapeKind.Collection);
                current = elementType;
                continue;
            }

            shape.Add(TypeUseShapeKind.Named);
            return new TypeUseDefinition
            {
                Name = TypeName(current),
                ObservedTypeSubject = current is INamedTypeSymbol named ? context.SubjectForType(named) : null,
                Shape = shape
            };
        }
    }

    /// <summary>
    /// Gets the public readable instance properties of a source type in declaration order.
    /// </summary>
    /// <param name="type">The source type.</param>
    /// <returns>The property definitions.</returns>
    public static IReadOnlyList<PropertyDefinition> PropertiesOf(INamedTypeSymbol type) => PropertiesOf(type, TypeReferenceFor);

    /// <summary>
    /// Gets public readable instance properties with exact source subjects when the analyzed projects own their types.
    /// </summary>
    /// <param name="type">The source type.</param>
    /// <param name="context">The analyzed project context used to resolve source subjects.</param>
    /// <returns>The property definitions.</returns>
    public static IReadOnlyList<PropertyDefinition> PropertiesOf(INamedTypeSymbol type, DotNetAnalysisContext context) =>
        PropertiesOf(type, propertyType => TypeReferenceFor(propertyType, context));

    internal static IReadOnlyList<IPropertySymbol> PublicReadablePropertiesOf(INamedTypeSymbol type) =>
    [
        .. type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(_ => !_.IsStatic && !_.IsIndexer && _.DeclaredAccessibility == Accessibility.Public && _.GetMethod?.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(SourceOrder)
            .ThenBy(_ => _.Name, StringComparer.Ordinal)
    ];

    internal static string PropertyName(string name) => name.Length == 0
        ? name
        : $"{char.ToLowerInvariant(name[0])}{name[1..]}";

    static TypeReferenceDefinition CreateTypeReference(
        ITypeSymbol type,
        Func<INamedTypeSymbol, SubjectId?>? subjectForType)
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
            Subject = elementType is INamedTypeSymbol named ? subjectForType?.Invoke(named) : null,
            IsCollection = isCollection,
            IsOptional = isOptional
        };
    }

    static IReadOnlyList<PropertyDefinition> PropertiesOf(
        INamedTypeSymbol type,
        Func<ITypeSymbol, TypeReferenceDefinition> typeReferenceFor) =>
    [
        .. PublicReadablePropertiesOf(type)
            .Select(_ => new PropertyDefinition
            {
                Name = PropertyName(_.Name),
                Type = typeReferenceFor(_.Type)
            })
    ];

    static (ITypeSymbol Type, bool IsCollection) CollectionElementOf(ITypeSymbol type) =>
        DotNetSymbols.CollectionElementOf(type);

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
}

/// <summary>
/// Provides metadata-name-based Roslyn symbol matching without loading framework assemblies.
/// </summary>
public static class DotNetSymbols
{
    /// <summary>
    /// Gets the collection element type for an array or <see cref="IEnumerable{T}"/>-shaped type.
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns>The collection element type, or <paramref name="type"/> when it is not a supported collection.</returns>
    public static ITypeSymbol ElementTypeOf(ITypeSymbol type) => CollectionElementOf(type).Type;

    /// <summary>
    /// Tries to read a typed named argument from an attribute.
    /// </summary>
    /// <typeparam name="T">The expected runtime value type.</typeparam>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The exact named argument.</param>
    /// <param name="value">The typed value when present and compatible.</param>
    /// <returns><see langword="true"/> when the named argument has the expected type; otherwise, <see langword="false"/>.</returns>
    public static bool TryNamedArgument<T>(AttributeData attribute, string name, out T value)
    {
        foreach (var argument in attribute.NamedArguments)
        {
            if (!string.Equals(argument.Key, name, StringComparison.Ordinal))
            {
                continue;
            }

            if (argument.Value.Value is T typedValue)
            {
                value = typedValue;
                return true;
            }

            if (argument.Value.IsNull && default(T) is null)
            {
                value = default!;
                return true;
            }

            break;
        }

        value = default!;
        return false;
    }

    /// <summary>
    /// Reads an optional typed value named argument from an attribute.
    /// </summary>
    /// <typeparam name="T">The expected value type.</typeparam>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The exact named argument.</param>
    /// <returns>The typed value, or <see langword="null"/> when the argument is missing or has another type.</returns>
    public static T? NamedArgument<T>(AttributeData attribute, string name)
        where T : struct =>
        TryNamedArgument(attribute, name, out T value) ? value : null;

    /// <summary>
    /// Reads a typed named argument from an attribute or returns an explicit fallback.
    /// </summary>
    /// <typeparam name="T">The expected runtime value type.</typeparam>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <param name="name">The exact named argument.</param>
    /// <param name="fallback">The value returned when the argument is missing or has another type.</param>
    /// <returns>The typed named argument or <paramref name="fallback"/>.</returns>
    public static T NamedArgument<T>(AttributeData attribute, string name, T fallback) =>
        TryNamedArgument(attribute, name, out T value) ? value : fallback;

    /// <summary>
    /// Gets methods on one type whose names are allowlisted and whose first parameter is an exact request type.
    /// </summary>
    /// <param name="containingType">The type that must declare every companion method.</param>
    /// <param name="requestType">The exact first-parameter request or message type.</param>
    /// <param name="names">The explicit companion method name set.</param>
    /// <returns>The matching methods in deterministic signature order.</returns>
    public static IReadOnlyList<IMethodSymbol> CompanionMethodsFor(
        INamedTypeSymbol containingType,
        ITypeSymbol requestType,
        IEnumerable<string> names)
    {
        var allowedNames = names.ToHashSet(StringComparer.Ordinal);
        return
        [
            .. containingType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(_ =>
                    _.MethodKind == MethodKind.Ordinary &&
                    !_.IsImplicitlyDeclared &&
                    allowedNames.Contains(_.Name) &&
                    _.Parameters.Length > 0 &&
                    SymbolEqualityComparer.Default.Equals(_.Parameters[0].Type, requestType))
                .OrderBy(_ => _.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), StringComparer.Ordinal)
        ];
    }

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

    internal static (ITypeSymbol Type, bool IsCollection) CollectionElementOf(ITypeSymbol type)
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
}
