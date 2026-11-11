// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Extracts exact values from a deliberately bounded subset of authored .NET expressions.
/// </summary>
public static class DotNetSourceValues
{
    static readonly FrozenDictionary<SpecialType, Type> _runtimeTypes = new Dictionary<SpecialType, Type>
    {
        [SpecialType.System_Boolean] = typeof(bool),
        [SpecialType.System_Byte] = typeof(byte),
        [SpecialType.System_SByte] = typeof(sbyte),
        [SpecialType.System_Int16] = typeof(short),
        [SpecialType.System_UInt16] = typeof(ushort),
        [SpecialType.System_Int32] = typeof(int),
        [SpecialType.System_UInt32] = typeof(uint),
        [SpecialType.System_Int64] = typeof(long),
        [SpecialType.System_UInt64] = typeof(ulong),
        [SpecialType.System_Single] = typeof(float),
        [SpecialType.System_Double] = typeof(double),
        [SpecialType.System_Decimal] = typeof(decimal),
        [SpecialType.System_Char] = typeof(char),
        [SpecialType.System_String] = typeof(string)
    }.ToFrozenDictionary();

    /// <summary>
    /// Extracts an exact semantic constant, <see langword="typeof"/> value, payload, or collection.
    /// </summary>
    /// <param name="expression">The authored expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact value, or deterministic failures with no partial value.</returns>
    public static DotNetBounded<DotNetSourceValue> Extract(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        if (ConditionalExpressionWithin(expression) is { } conditionalExpression)
        {
            return Unknown<DotNetSourceValue>(conditionalExpression, DotNetValueFailureKind.Conditional, "The value depends on a condition");
        }

        var unwrapped = UnwrapParentheses(expression);
        return unwrapped is TypeOfExpressionSyntax typeOf
            ? ExtractTypeOf(typeOf, expression, semanticModel)
            : ExtractValue(unwrapped, expression, semanticModel);
    }

    /// <summary>
    /// Extracts one exact semantic constant of the requested runtime value type.
    /// </summary>
    /// <typeparam name="T">The requested constant value type.</typeparam>
    /// <param name="expression">The authored expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact primitive, string, or null constant, or deterministic failures. Source enum constants are available through <see cref="Extract(ExpressionSyntax, SemanticModel)"/> as exact field symbols rather than runtime enum values.</returns>
    public static DotNetBounded<T> Constant<T>(ExpressionSyntax expression, SemanticModel semanticModel) =>
        Extract(expression, semanticModel) switch
        {
            DotNetKnown<DotNetSourceValue> { Value: DotNetConstantValue constant } when TryRuntimeConstant(constant, out T? value) => new DotNetKnown<T>(value!),
            DotNetUnknown<DotNetSourceValue> unknown => new DotNetUnknown<T>(unknown.Failures),
            _ => Unknown<T>(expression, DotNetValueFailureKind.Unsupported, "The exact value is not the requested primitive, string, or null constant type")
        };

    /// <summary>
    /// Extracts one exact constructed payload.
    /// </summary>
    /// <param name="expression">The authored construction.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact payload, or deterministic failures.</returns>
    public static DotNetBounded<DotNetPayloadValue> Payload(ExpressionSyntax expression, SemanticModel semanticModel) =>
        Extract(expression, semanticModel) switch
        {
            DotNetKnown<DotNetSourceValue> { Value: DotNetPayloadValue value } => new DotNetKnown<DotNetPayloadValue>(value),
            DotNetUnknown<DotNetSourceValue> unknown => new DotNetUnknown<DotNetPayloadValue>(unknown.Failures),
            _ => Unknown<DotNetPayloadValue>(expression, DotNetValueFailureKind.Unsupported, "The exact value is not a constructed payload")
        };

    /// <summary>
    /// Extracts one exact authored collection.
    /// </summary>
    /// <param name="expression">The authored collection expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact collection, or deterministic failures.</returns>
    public static DotNetBounded<DotNetCollectionValue> Collection(ExpressionSyntax expression, SemanticModel semanticModel) =>
        Extract(expression, semanticModel) switch
        {
            DotNetKnown<DotNetSourceValue> { Value: DotNetCollectionValue value } => new DotNetKnown<DotNetCollectionValue>(value),
            DotNetUnknown<DotNetSourceValue> unknown => new DotNetUnknown<DotNetCollectionValue>(unknown.Failures),
            _ => Unknown<DotNetCollectionValue>(expression, DotNetValueFailureKind.Unsupported, "The exact value is not an authored collection")
        };

    /// <summary>
    /// Extracts one exact type named by <see langword="typeof"/>.
    /// </summary>
    /// <param name="expression">The authored expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact type, or deterministic failures.</returns>
    public static DotNetBounded<ITypeSymbol> TypeOf(ExpressionSyntax expression, SemanticModel semanticModel) =>
        Extract(expression, semanticModel) switch
        {
            DotNetKnown<DotNetSourceValue> { Value: DotNetTypeValue value } => new DotNetKnown<ITypeSymbol>(value.Type),
            DotNetUnknown<DotNetSourceValue> unknown => new DotNetUnknown<ITypeSymbol>(unknown.Failures),
            _ => Unknown<ITypeSymbol>(expression, DotNetValueFailureKind.Unsupported, "The exact value is not a typeof expression")
        };

    internal static DotNetUnknown<T> Unknown<T>(
        ExpressionSyntax expression,
        DotNetValueFailureKind kind,
        string message) =>
        new([new(kind, expression.GetLocation(), message)]);

    internal static DotNetValueFailure? SemanticTypeFailure(
        SyntaxNode source,
        string description,
        bool requiresType,
        params ITypeSymbol?[] types)
    {
        var boundTypes = types.Where(type => type is not null).Cast<ITypeSymbol>().ToArray();
        return SemanticTypeFailureKind(boundTypes, requiresType) switch
        {
            DotNetValueFailureKind.Dynamic => new(DotNetValueFailureKind.Dynamic, source.GetLocation(), $"The {description} is dynamically bound"),
            DotNetValueFailureKind.Unbound => new(DotNetValueFailureKind.Unbound, source.GetLocation(), $"The {description} has no exact error-free binding"),
            _ => null
        };
    }

    internal static bool HasInvalidOperation(IOperation? operation) =>
        operation is null or IInvalidOperation ||
        (operation is IConversionOperation conversion && IsBrokenConversion(conversion.Conversion)) ||
        operation.ChildOperations.Any(HasInvalidOperation);

    internal static DotNetValueFailure? CompilerErrorFailure(
        SyntaxNode source,
        SemanticModel semanticModel,
        string description) =>
        semanticModel.GetDiagnostics(source.Span).Any(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.Location.IsInSource &&
            diagnostic.Location.SourceTree == source.SyntaxTree &&
            source.Span.Contains(diagnostic.Location.SourceSpan))
                ? new(DotNetValueFailureKind.Unsupported, source.GetLocation(), $"The authored {description} contains a compiler error")
                : null;

    internal static DotNetValueFailure? ConstructionRootFailure(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel)
    {
        var authoredChildren = creation.ArgumentList?.Arguments.Select(argument => (SyntaxNode)argument.Expression).ToList() ?? [];
        authoredChildren.AddRange(creation.Initializer?.Expressions ?? []);
        var hasRootError = semanticModel.GetDiagnostics(creation.Span)
            .Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Location.IsInSource &&
                diagnostic.Location.SourceTree == creation.SyntaxTree &&
                creation.Span.Contains(diagnostic.Location.SourceSpan) &&
                !authoredChildren.Exists(child => child.Span.Contains(diagnostic.Location.SourceSpan)));

        return hasRootError
            ? new(DotNetValueFailureKind.Unsupported, creation.GetLocation(), "The construction root is not semantically valid")
            : null;
    }

    internal static void AddFailure(
        List<DotNetValueFailure> failures,
        DotNetValueFailure failure)
    {
        if (!failures.Exists(existing => IsSameFailureLocation(existing, failure)))
        {
            failures.Add(failure);
        }
    }

    internal static void AddFailures(
        List<DotNetValueFailure> failures,
        IEnumerable<DotNetValueFailure> additions)
    {
        foreach (var failure in additions)
        {
            AddFailure(failures, failure);
        }
    }

    internal static DotNetValueFailure? ContextualConversionFailure(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        string description,
        bool validateOperation = true)
    {
        var convertedType = semanticModel.GetTypeInfo(expression).ConvertedType;
        if (convertedType is null)
        {
            return null;
        }

        var conversion = validateOperation
            ? semanticModel.ClassifyConversion(expression, convertedType)
            : semanticModel.GetConversion(expression);
        var operation = semanticModel.GetOperation(expression);

        return !IsSupportedContextualConversion(conversion) ||
            (validateOperation && HasInvalidContextualOperation(operation, expression))
                ? new(DotNetValueFailureKind.Unsupported, expression.GetLocation(), $"The authored {description} conversion is not semantically valid")
                : null;
    }

    internal static bool IsSupportedContextualConversion(Conversion conversion) =>
        conversion.Exists &&
        (conversion.IsIdentity || conversion.IsImplicit) &&
        !conversion.IsUserDefined;

    internal static ExpressionSyntax? ConditionalExpressionWithin(ExpressionSyntax expression)
    {
        while (WrappedExpression(expression) is { } inner)
        {
            expression = inner;
        }

        return IsConditionalShape(expression) ? expression : null;
    }

    static ExpressionSyntax? WrappedExpression(ExpressionSyntax expression) => expression switch
    {
        ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
        CastExpressionSyntax cast => cast.Expression,
        CheckedExpressionSyntax checkedExpression => checkedExpression.Expression,
        PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression } suppression => suppression.Operand,
        _ => null
    };

    static bool IsConditionalShape(ExpressionSyntax expression) => expression is
        ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax or SwitchExpressionSyntax or
        BinaryExpressionSyntax
        {
            RawKind: (int)SyntaxKind.CoalesceExpression or
                    (int)SyntaxKind.LogicalAndExpression or
                    (int)SyntaxKind.LogicalOrExpression
        } or
        AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceAssignmentExpression };

    static bool HasInvalidContextualOperation(
        IOperation? operation,
        ExpressionSyntax expression) =>
        operation is null or IInvalidOperation ||
        (operation is IConversionOperation conversion && IsBrokenConversion(conversion.Conversion)) ||
        HasInvalidContextualParent(operation, expression);

    static bool HasInvalidContextualParent(IOperation operation, ExpressionSyntax expression) => operation.Parent switch
    {
        IInvalidOperation => true,
        IConversionOperation parentConversion =>
            parentConversion.Syntax.Span == expression.Span && IsBrokenConversion(parentConversion.Conversion),
        IArgumentOperation argument => IsBrokenConversion(argument.InConversion),
        _ => false
    };

    static bool IsBrokenConversion(CommonConversion conversion) => !conversion.Exists || conversion.IsUserDefined;

    static bool TryRuntimeConstant<T>(DotNetConstantValue constant, out T? value)
    {
        value = default;
        if (constant.Value is null)
        {
            return default(T) is null;
        }

        if (constant.Value is IFieldSymbol || typeof(T).IsEnum)
        {
            return false;
        }

        var runtimeType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (!MatchesRuntimeType(constant.Type, runtimeType))
        {
            return false;
        }

        if (constant.Value is T exact)
        {
            value = exact;
            return true;
        }

        return constant.Value is IConvertible && TryChangeType(constant.Value, runtimeType, out value);
    }

    static bool TryChangeType<T>(object constantValue, Type runtimeType, out T? value)
    {
        value = default;
        try
        {
            value = (T)Convert.ChangeType(constantValue, runtimeType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }
    }

    static DotNetValueFailureKind? SemanticTypeFailureKind(ITypeSymbol[] boundTypes, bool requiresType)
    {
        if (boundTypes.Any(type => ContainsSemanticTypeKind(type, TypeKind.Dynamic)))
        {
            return DotNetValueFailureKind.Dynamic;
        }

        return (requiresType && boundTypes.Length == 0) || boundTypes.Any(type => ContainsSemanticTypeKind(type, TypeKind.Error))
            ? DotNetValueFailureKind.Unbound
            : null;
    }

    static bool IsSameFailureLocation(DotNetValueFailure existing, DotNetValueFailure failure) =>
        existing.Kind == failure.Kind &&
        existing.Source.SourceTree == failure.Source.SourceTree &&
        existing.Source.SourceSpan == failure.Source.SourceSpan;

    static DotNetBounded<DotNetSourceValue> ExtractValue(
        ExpressionSyntax expression,
        ExpressionSyntax authoredExpression,
        SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(authoredExpression);
        if (DotNetCollectionValues.TryExtract(expression, semanticModel) is { } collection)
        {
            return collection;
        }

        if (expression is BaseObjectCreationExpressionSyntax creation)
        {
            return DotNetPayloadValues.Extract(creation, semanticModel);
        }

        return SemanticTypeFailure(expression, "value", false, typeInfo.Type, typeInfo.ConvertedType) is { } typeFailure
            ? new DotNetUnknown<DotNetSourceValue>([typeFailure])
            : ExtractBoundValue(expression, authoredExpression, semanticModel, typeInfo);
    }

    static DotNetBounded<DotNetSourceValue> ExtractBoundValue(
        ExpressionSyntax expression,
        ExpressionSyntax authoredExpression,
        SemanticModel semanticModel,
        TypeInfo typeInfo)
    {
        var symbol = semanticModel.GetSymbolInfo(expression);
        if (symbol.Symbol is null && symbol.CandidateReason == CandidateReason.Ambiguous)
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Ambiguous, "The value has ambiguous symbol binding");
        }

        var operation = semanticModel.GetOperation(authoredExpression);
        var constant = operation is { ConstantValue.HasValue: true } ? operation.ConstantValue : semanticModel.GetConstantValue(authoredExpression);
        if (!constant.HasValue)
        {
            return UnknownWithoutConstant(expression, symbol.Symbol);
        }

        if (ConstantConversionFailure(authoredExpression, semanticModel) is { } constantFailure)
        {
            return new DotNetUnknown<DotNetSourceValue>([constantFailure]);
        }

        return ExtractConstantValue(new(expression, typeInfo, symbol.Symbol, operation, constant.Value));
    }

    static DotNetBounded<DotNetSourceValue> ExtractTypeOf(
        TypeOfExpressionSyntax typeOf,
        ExpressionSyntax authoredExpression,
        SemanticModel semanticModel)
    {
        var operandTypeInfo = semanticModel.GetTypeInfo(typeOf.Type);
        var typeSymbol = semanticModel.GetSymbolInfo(typeOf.Type);
        var failure = typeSymbol.CandidateReason == CandidateReason.Ambiguous
            ? new DotNetValueFailure(DotNetValueFailureKind.Ambiguous, typeOf.GetLocation(), "The typeof operand has ambiguous binding")
            : null;
        failure ??= SemanticTypeFailure(typeOf, "typeof operand", true, operandTypeInfo.Type, operandTypeInfo.ConvertedType);
        failure ??= TypeOfValueFailure(typeOf, authoredExpression, semanticModel);
        return failure is null
            ? new DotNetKnown<DotNetSourceValue>(new DotNetTypeValue(operandTypeInfo.Type ?? operandTypeInfo.ConvertedType!))
            : new DotNetUnknown<DotNetSourceValue>([failure]);
    }

    static DotNetValueFailure? TypeOfValueFailure(
        TypeOfExpressionSyntax typeOf,
        ExpressionSyntax authoredExpression,
        SemanticModel semanticModel)
    {
        var valueTypeInfo = semanticModel.GetTypeInfo(authoredExpression);
        return SemanticTypeFailure(typeOf, "typeof value", true, valueTypeInfo.Type, valueTypeInfo.ConvertedType) ??
            ContextualConversionFailure(authoredExpression, semanticModel, "typeof value") ??
            CompilerErrorFailure(authoredExpression, semanticModel, "typeof value");
    }

    static DotNetValueFailure? ConstantConversionFailure(ExpressionSyntax authoredExpression, SemanticModel semanticModel) =>
        ContextualConversionFailure(authoredExpression, semanticModel, "value") ??
        CompilerErrorFailure(authoredExpression, semanticModel, "value");

    static DotNetBounded<DotNetSourceValue> ExtractConstantValue(ConstantSite site)
    {
        var sourceType = site.TypeInfo.Type ?? site.Operation?.Type;
        var enumType = site.Value is null
            ? null
            : EnumTypeFor(sourceType) ?? EnumTypeFor(site.TypeInfo.ConvertedType);
        if (enumType is not null)
        {
            return ExtractEnumConstant(site, enumType);
        }

        return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(site.Value, site.TypeInfo.ConvertedType ?? sourceType));
    }

    static DotNetBounded<DotNetSourceValue> ExtractEnumConstant(ConstantSite site, INamedTypeSymbol enumType)
    {
        if (site.Symbol is IFieldSymbol { HasConstantValue: true } direct && IsDirectEnumField(direct, enumType, site.Value))
        {
            return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(direct, enumType));
        }

        var members = enumType.GetMembers()
            .OfType<IFieldSymbol>()
            .Where(field => field.HasConstantValue && Equals(field.ConstantValue, site.Value))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if (members.Length != 1)
        {
            var kind = members.Length > 1 ? DotNetValueFailureKind.Ambiguous : DotNetValueFailureKind.Unsupported;
            return Unknown<DotNetSourceValue>(site.Expression, kind, kind == DotNetValueFailureKind.Ambiguous
                ? "The enum constant names several declared members"
                : "The enum constant names no declared member");
        }

        return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(members[0], enumType));
    }

    static bool IsDirectEnumField(IFieldSymbol field, INamedTypeSymbol enumType, object? value) =>
        SymbolEqualityComparer.Default.Equals(field.ContainingType, enumType) &&
        Equals(field.ConstantValue, value);

    static DotNetUnknown<DotNetSourceValue> UnknownWithoutConstant(ExpressionSyntax expression, ISymbol? symbol)
    {
        if (symbol is null && RequiresBinding(expression))
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Unbound, "The value has no exact bound symbol");
        }

        return IsComputedShape(expression)
            ? Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Computed, "The value is computed by executable code")
            : Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Unsupported, "The authored value shape is outside bounded extraction");
    }

    static bool IsComputedShape(ExpressionSyntax expression) => expression is
        InvocationExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or
        ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax or AwaitExpressionSyntax or
        BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax or InterpolatedStringExpressionSyntax;

    static INamedTypeSymbol? EnumTypeFor(ITypeSymbol? type)
    {
        if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
        {
            return enumType;
        }

        return type is INamedTypeSymbol
        {
            OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
            TypeArguments: [INamedTypeSymbol { TypeKind: TypeKind.Enum } nullableEnum]
        }
            ? nullableEnum
            : null;
    }

    static bool ContainsSemanticTypeKind(ITypeSymbol type, TypeKind kind) =>
        type.TypeKind == kind || type switch
        {
            IArrayTypeSymbol array => ContainsSemanticTypeKind(array.ElementType, kind),
            IFunctionPointerTypeSymbol functionPointer => SignatureContainsSemanticTypeKind(functionPointer, kind),
            INamedTypeSymbol named => NamedTypeContainsSemanticTypeKind(named, kind),
            IPointerTypeSymbol pointer => ContainsSemanticTypeKind(pointer.PointedAtType, kind),
            _ => false
        };

    static bool SignatureContainsSemanticTypeKind(IFunctionPointerTypeSymbol functionPointer, TypeKind kind) =>
        ContainsSemanticTypeKind(functionPointer.Signature.ReturnType, kind) ||
        functionPointer.Signature.Parameters.Any(parameter => ContainsSemanticTypeKind(parameter.Type, kind));

    static bool NamedTypeContainsSemanticTypeKind(INamedTypeSymbol named, TypeKind kind) =>
        (named.ContainingType is not null && ContainsSemanticTypeKind(named.ContainingType, kind)) ||
        named.TypeArguments.Any(typeArgument => ContainsSemanticTypeKind(typeArgument, kind));

    static bool MatchesRuntimeType(ITypeSymbol? sourceType, Type runtimeType) =>
        sourceType is not null &&
        _runtimeTypes.TryGetValue(sourceType.SpecialType, out var expected) &&
        runtimeType == expected;

    static bool RequiresBinding(ExpressionSyntax expression) => expression is
        IdentifierNameSyntax or GenericNameSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or InvocationExpressionSyntax;

    static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }

    readonly record struct ConstantSite(
        ExpressionSyntax Expression,
        TypeInfo TypeInfo,
        ISymbol? Symbol,
        IOperation? Operation,
        object? Value);
}
