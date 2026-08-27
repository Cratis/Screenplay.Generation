// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
    /// <summary>
    /// Extracts an exact semantic constant, <c>typeof</c> value, payload, or collection.
    /// </summary>
    /// <param name="expression">The authored expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact value, or deterministic failures with no partial value.</returns>
    public static DotNetBounded<DotNetSourceValue> Extract(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        var authoredExpression = expression;
        if (ConditionalExpressionWithin(expression) is { } conditionalExpression)
        {
            return Unknown<DotNetSourceValue>(conditionalExpression, DotNetValueFailureKind.Conditional, "The value depends on a condition");
        }

        expression = UnwrapParentheses(expression);
        if (expression is TypeOfExpressionSyntax typeOf)
        {
            var operandTypeInfo = semanticModel.GetTypeInfo(typeOf.Type);
            var typeSymbol = semanticModel.GetSymbolInfo(typeOf.Type);
            if (typeSymbol.CandidateReason == CandidateReason.Ambiguous)
            {
                return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Ambiguous, "The typeof operand has ambiguous binding");
            }

            if (SemanticTypeFailure(expression, "typeof operand", true, operandTypeInfo.Type, operandTypeInfo.ConvertedType) is { } failure)
            {
                return new DotNetUnknown<DotNetSourceValue>([failure]);
            }

            var valueTypeInfo = semanticModel.GetTypeInfo(authoredExpression);
            if (SemanticTypeFailure(
                    expression,
                    "typeof value",
                    true,
                    valueTypeInfo.Type,
                    valueTypeInfo.ConvertedType) is { } valueTypeFailure)
            {
                return new DotNetUnknown<DotNetSourceValue>([valueTypeFailure]);
            }

            if (ContextualConversionFailure(authoredExpression, semanticModel, "typeof value") is { } conversionFailure)
            {
                return new DotNetUnknown<DotNetSourceValue>([conversionFailure]);
            }

            if (CompilerErrorFailure(authoredExpression, semanticModel, "typeof value") is { } compilerFailure)
            {
                return new DotNetUnknown<DotNetSourceValue>([compilerFailure]);
            }

            return new DotNetKnown<DotNetSourceValue>(new DotNetTypeValue(operandTypeInfo.Type ?? operandTypeInfo.ConvertedType!));
        }

        var typeInfo = semanticModel.GetTypeInfo(authoredExpression);
        if (DotNetCollectionValues.TryExtract(expression, semanticModel) is { } collection)
        {
            return collection;
        }

        if (expression is BaseObjectCreationExpressionSyntax creation)
        {
            return DotNetPayloadValues.Extract(creation, semanticModel);
        }

        if (SemanticTypeFailure(expression, "value", false, typeInfo.Type, typeInfo.ConvertedType) is { } typeFailure)
        {
            return new DotNetUnknown<DotNetSourceValue>([typeFailure]);
        }

        var symbol = semanticModel.GetSymbolInfo(expression);
        if (symbol.Symbol is null && symbol.CandidateReason == CandidateReason.Ambiguous)
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Ambiguous, "The value has ambiguous symbol binding");
        }

        var operation = semanticModel.GetOperation(authoredExpression);
        var constant = operation is { ConstantValue.HasValue: true } ? operation.ConstantValue : semanticModel.GetConstantValue(authoredExpression);
        if (constant.HasValue)
        {
            if (ContextualConversionFailure(authoredExpression, semanticModel, "value") is { } conversionFailure)
            {
                return new DotNetUnknown<DotNetSourceValue>([conversionFailure]);
            }

            if (CompilerErrorFailure(authoredExpression, semanticModel, "value") is { } compilerFailure)
            {
                return new DotNetUnknown<DotNetSourceValue>([compilerFailure]);
            }

            var sourceType = typeInfo.Type ?? operation?.Type;
            var enumType = constant.Value is null
                ? null
                : EnumTypeFor(sourceType) ?? EnumTypeFor(typeInfo.ConvertedType);
            var type = enumType ?? typeInfo.ConvertedType ?? sourceType;
            if (enumType is not null)
            {
                if (symbol.Symbol is IFieldSymbol { HasConstantValue: true } direct &&
                    SymbolEqualityComparer.Default.Equals(direct.ContainingType, enumType) &&
                    Equals(direct.ConstantValue, constant.Value))
                {
                    return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(direct, enumType));
                }

                var members = enumType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(field => field.HasConstantValue && Equals(field.ConstantValue, constant.Value))
                    .OrderBy(field => field.Name, StringComparer.Ordinal)
                    .ToArray();
                if (members.Length != 1)
                {
                    var kind = members.Length > 1 ? DotNetValueFailureKind.Ambiguous : DotNetValueFailureKind.Unsupported;
                    return Unknown<DotNetSourceValue>(expression, kind, kind == DotNetValueFailureKind.Ambiguous
                        ? "The enum constant names several declared members"
                        : "The enum constant names no declared member");
                }

                return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(members[0], enumType));
            }

            return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(constant.Value, type));
        }

        if (symbol.Symbol is null && RequiresBinding(expression))
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Unbound, "The value has no exact bound symbol");
        }

        if (expression is InvocationExpressionSyntax or MemberAccessExpressionSyntax or ElementAccessExpressionSyntax or
            ObjectCreationExpressionSyntax or ImplicitObjectCreationExpressionSyntax or AwaitExpressionSyntax or
            BinaryExpressionSyntax or PrefixUnaryExpressionSyntax or PostfixUnaryExpressionSyntax or InterpolatedStringExpressionSyntax)
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Computed, "The value is computed by executable code");
        }

        return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Unsupported, "The authored value shape is outside bounded extraction");
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
    /// Extracts one exact type named by <c>typeof</c>.
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
        DotNetValueFailureKind? kind = null;
        if (boundTypes.Any(type => ContainsSemanticTypeKind(type, TypeKind.Dynamic)))
        {
            kind = DotNetValueFailureKind.Dynamic;
        }
        else if ((requiresType && boundTypes.Length == 0) || boundTypes.Any(type => ContainsSemanticTypeKind(type, TypeKind.Error)))
        {
            kind = DotNetValueFailureKind.Unbound;
        }

        return kind switch
        {
            DotNetValueFailureKind.Dynamic => new(kind.Value, source.GetLocation(), $"The {description} is dynamically bound"),
            DotNetValueFailureKind.Unbound => new(kind.Value, source.GetLocation(), $"The {description} has no exact error-free binding"),
            _ => null
        };
    }

    internal static bool HasInvalidOperation(IOperation? operation) =>
        operation is null or IInvalidOperation ||
        (operation is IConversionOperation conversion &&
            (!conversion.Conversion.Exists || conversion.Conversion.IsUserDefined)) ||
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
        if (!failures.Exists(existing => existing.Kind == failure.Kind &&
            existing.Source.SourceTree == failure.Source.SourceTree &&
            existing.Source.SourceSpan == failure.Source.SourceSpan))
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
        while (expression is ParenthesizedExpressionSyntax or CastExpressionSyntax or CheckedExpressionSyntax or
            PostfixUnaryExpressionSyntax { RawKind: (int)SyntaxKind.SuppressNullableWarningExpression })
        {
            expression = expression switch
            {
                ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
                CastExpressionSyntax cast => cast.Expression,
                CheckedExpressionSyntax checkedExpression => checkedExpression.Expression,
                PostfixUnaryExpressionSyntax suppression => suppression.Operand,
                _ => expression
            };
        }

        return expression is ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax or SwitchExpressionSyntax or
            BinaryExpressionSyntax
            {
                RawKind: (int)SyntaxKind.CoalesceExpression or
                    (int)SyntaxKind.LogicalAndExpression or
                    (int)SyntaxKind.LogicalOrExpression
            } or
            AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceAssignmentExpression }
                ? expression
                : null;
    }

    static bool HasInvalidContextualOperation(
        IOperation? operation,
        ExpressionSyntax expression)
    {
        if (operation is null or IInvalidOperation)
        {
            return true;
        }

        if (operation is IConversionOperation conversion &&
            (!conversion.Conversion.Exists || conversion.Conversion.IsUserDefined))
        {
            return true;
        }

        if (operation.Parent is IInvalidOperation)
        {
            return true;
        }

        if (operation.Parent is IConversionOperation parentConversion &&
            parentConversion.Syntax.Span == expression.Span &&
            (!parentConversion.Conversion.Exists || parentConversion.Conversion.IsUserDefined))
        {
            return true;
        }

        return operation.Parent is IArgumentOperation argument &&
            (!argument.InConversion.Exists || argument.InConversion.IsUserDefined);
    }

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

        if (constant.Value is not IConvertible)
        {
            return false;
        }

        try
        {
            value = (T)Convert.ChangeType(constant.Value, runtimeType, CultureInfo.InvariantCulture);
            return true;
        }
        catch (Exception exception) when (exception is InvalidCastException or FormatException or OverflowException)
        {
            return false;
        }
    }

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
            IFunctionPointerTypeSymbol functionPointer =>
                ContainsSemanticTypeKind(functionPointer.Signature.ReturnType, kind) ||
                functionPointer.Signature.Parameters.Any(parameter => ContainsSemanticTypeKind(parameter.Type, kind)),
            INamedTypeSymbol named =>
                (named.ContainingType is not null && ContainsSemanticTypeKind(named.ContainingType, kind)) ||
                named.TypeArguments.Any(typeArgument => ContainsSemanticTypeKind(typeArgument, kind)),
            IPointerTypeSymbol pointer => ContainsSemanticTypeKind(pointer.PointedAtType, kind),
            _ => false
        };

    static bool MatchesRuntimeType(ITypeSymbol? sourceType, Type runtimeType) => sourceType?.SpecialType switch
    {
        SpecialType.System_Boolean => runtimeType == typeof(bool),
        SpecialType.System_Byte => runtimeType == typeof(byte),
        SpecialType.System_SByte => runtimeType == typeof(sbyte),
        SpecialType.System_Int16 => runtimeType == typeof(short),
        SpecialType.System_UInt16 => runtimeType == typeof(ushort),
        SpecialType.System_Int32 => runtimeType == typeof(int),
        SpecialType.System_UInt32 => runtimeType == typeof(uint),
        SpecialType.System_Int64 => runtimeType == typeof(long),
        SpecialType.System_UInt64 => runtimeType == typeof(ulong),
        SpecialType.System_Single => runtimeType == typeof(float),
        SpecialType.System_Double => runtimeType == typeof(double),
        SpecialType.System_Decimal => runtimeType == typeof(decimal),
        SpecialType.System_Char => runtimeType == typeof(char),
        SpecialType.System_String => runtimeType == typeof(string),
        _ => false
    };

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
}
