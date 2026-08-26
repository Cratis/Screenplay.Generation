// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Represents one exact value recovered from bounded .NET source.
/// </summary>
public abstract record DotNetSourceValue;

/// <summary>
/// Represents one semantic constant and its exact source type.
/// </summary>
/// <param name="Value">The constant value, or an exact enum-member symbol.</param>
/// <param name="Type">The exact source or converted type.</param>
public sealed record DotNetConstantValue(object? Value, ITypeSymbol? Type) : DotNetSourceValue;

/// <summary>
/// Represents one exact type named by <c>typeof</c>.
/// </summary>
/// <param name="Type">The exact named type.</param>
public sealed record DotNetTypeValue(ITypeSymbol Type) : DotNetSourceValue;

/// <summary>
/// Extracts exact values from a deliberately bounded subset of authored .NET expressions.
/// </summary>
public static class DotNetSourceValues
{
    /// <summary>
    /// Extracts an exact semantic constant or <c>typeof</c> value.
    /// </summary>
    /// <param name="expression">The authored expression.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exact value, or deterministic failures with no partial value.</returns>
    public static DotNetBounded<DotNetSourceValue> Extract(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        expression = Unwrap(expression);
        if (expression is ConditionalExpressionSyntax or ConditionalAccessExpressionSyntax or SwitchExpressionSyntax or
            BinaryExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceExpression } or
            AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.CoalesceAssignmentExpression })
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Conditional, "The value depends on a condition");
        }

        if (expression is TypeOfExpressionSyntax typeOf)
        {
            var type = semanticModel.GetTypeInfo(typeOf.Type).Type;
            if (type is { TypeKind: not TypeKind.Error and not TypeKind.Dynamic })
            {
                return new DotNetKnown<DotNetSourceValue>(new DotNetTypeValue(type));
            }

            var typeSymbol = semanticModel.GetSymbolInfo(typeOf.Type);
            var kind = typeSymbol.CandidateSymbols.Length > 1 ? DotNetValueFailureKind.Ambiguous : DotNetValueFailureKind.Unbound;
            return Unknown<DotNetSourceValue>(expression, kind, kind == DotNetValueFailureKind.Ambiguous
                ? "The typeof operand has ambiguous binding"
                : "The typeof operand has no exact bound type");
        }

        var typeInfo = semanticModel.GetTypeInfo(expression);
        if (typeInfo.Type?.TypeKind == TypeKind.Dynamic || typeInfo.ConvertedType?.TypeKind == TypeKind.Dynamic)
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Dynamic, "The value is dynamically bound");
        }

        var symbol = semanticModel.GetSymbolInfo(expression);
        if (symbol.Symbol is null && symbol.CandidateSymbols.Length > 1)
        {
            return Unknown<DotNetSourceValue>(expression, DotNetValueFailureKind.Ambiguous, "The value has ambiguous symbol binding");
        }

        var operation = semanticModel.GetOperation(expression);
        var constant = operation is { ConstantValue.HasValue: true } ? operation.ConstantValue : semanticModel.GetConstantValue(expression);
        if (constant.HasValue)
        {
            var sourceType = typeInfo.Type ?? operation?.Type;
            var type = sourceType?.TypeKind == TypeKind.Enum
                ? sourceType
                : typeInfo.ConvertedType ?? sourceType;
            if (type?.TypeKind == TypeKind.Enum)
            {
                if (symbol.Symbol is IFieldSymbol { HasConstantValue: true } direct &&
                    SymbolEqualityComparer.Default.Equals(direct.ContainingType, type) &&
                    Equals(direct.ConstantValue, constant.Value))
                {
                    return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(direct, type));
                }

                var members = type.GetMembers()
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

                return new DotNetKnown<DotNetSourceValue>(new DotNetConstantValue(members[0], type));
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

    static DotNetUnknown<T> Unknown<T>(
        ExpressionSyntax expression,
        DotNetValueFailureKind kind,
        string message) =>
        new([new(kind, expression.GetLocation(), message)]);

    static ExpressionSyntax Unwrap(ExpressionSyntax expression)
    {
        while (expression is ParenthesizedExpressionSyntax parenthesized)
        {
            expression = parenthesized.Expression;
        }

        return expression;
    }
}
