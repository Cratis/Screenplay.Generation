// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Screenplay.Generation.DotNet;

internal static class DotNetCollectionValues
{
    public static DotNetBounded<DotNetSourceValue>? TryExtract(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        var failures = new List<DotNetValueFailure>();
        var hasUnsupportedRank = false;
        if (expression is ArrayCreationExpressionSyntax array)
        {
            if (array.Initializer is null)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    array.GetLocation(),
                    "An array without an initializer has no individually authored elements"));
            }

            hasUnsupportedRank = ValidateDimensions(array, semanticModel, failures);
        }

        if (expression is ImplicitArrayCreationExpressionSyntax { Commas.Count: > 0 } implicitMultidimensional)
        {
            hasUnsupportedRank = true;
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                implicitMultidimensional.GetLocation(),
                "Only one-dimensional arrays are supported by bounded collection extraction"));
        }

        IReadOnlyList<SyntaxNode>? elements = expression switch
        {
            ArrayCreationExpressionSyntax collectionArray => collectionArray.Initializer?.Expressions ?? [],
            ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
            InitializerExpressionSyntax initializer => initializer.Expressions,
            CollectionExpressionSyntax collection => collection.Elements,
            BaseObjectCreationExpressionSyntax { Initializer: { } initializer } collectionCreation
                when IsDirectCollectionInitializer(collectionCreation, semanticModel) => initializer.Expressions,
            _ => null
        };
        if (elements is null)
        {
            return null;
        }

        var typeInfo = semanticModel.GetTypeInfo(expression);
        var type = typeInfo.Type ?? typeInfo.ConvertedType;
        ValidateType(expression, typeInfo, failures);
        if (expression is not InitializerExpressionSyntax &&
            DotNetSourceValues.ContextualConversionFailure(
                expression,
                semanticModel,
                "collection value",
                false) is { } conversionFailure &&
            ShouldReportRootConversionFailure(expression, semanticModel))
        {
            DotNetSourceValues.AddFailure(failures, conversionFailure);
        }

        if (expression is BaseObjectCreationExpressionSyntax creation &&
            DotNetSourceValues.ConstructionRootFailure(creation, semanticModel) is { } rootFailure)
        {
            DotNetSourceValues.AddFailure(failures, rootFailure);
        }

        var values = new List<DotNetCollectionElement>();
        foreach (var element in elements)
        {
            if (element is SpreadElementSyntax spread)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.OpaqueSpread,
                    spread.GetLocation(),
                    "A collection spread hides elements that are not authored individually"));
                CollectSpreadFailures(spread.Expression, semanticModel, failures);
                continue;
            }

            var elementExpression = element switch
            {
                ExpressionElementSyntax expressionElement => expressionElement.Expression,
                ExpressionSyntax direct => direct,
                _ => null
            };
            if (elementExpression is null)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    element.GetLocation(),
                    "The collection element shape is outside bounded extraction"));
                continue;
            }

            if (elementExpression is InitializerExpressionSyntax nestedInitializer)
            {
                if (expression is BaseObjectCreationExpressionSyntax && nestedInitializer.Expressions.Count == 1)
                {
                    elementExpression = nestedInitializer.Expressions[0];
                }
                else
                {
                    if (!hasUnsupportedRank)
                    {
                        DotNetSourceValues.AddFailure(failures, new(
                            DotNetValueFailureKind.Unsupported,
                            nestedInitializer.GetLocation(),
                            expression is BaseObjectCreationExpressionSyntax
                                ? "A direct collection initializer entry must contain one authored value"
                                : "A nested collection initializer is outside bounded extraction"));
                    }

                    CollectInitializerFailures(
                        nestedInitializer,
                        semanticModel,
                        failures,
                        !hasUnsupportedRank);
                    continue;
                }
            }

            if (DotNetSourceValues.ContextualConversionFailure(
                    elementExpression,
                    semanticModel,
                    "collection element") is { } elementConversionFailure)
            {
                DotNetSourceValues.AddFailure(failures, elementConversionFailure);
            }

            switch (DotNetSourceValues.Extract(elementExpression, semanticModel))
            {
                case DotNetKnown<DotNetSourceValue> known:
                    values.Add(new(known.Value, elementExpression.GetLocation()));
                    break;
                case DotNetUnknown<DotNetSourceValue> unknown:
                    DotNetSourceValues.AddFailures(failures, unknown.Failures);
                    break;
            }
        }

        if (failures.Count == 0 &&
            DotNetSourceValues.CompilerErrorFailure(expression, semanticModel, "collection") is { } compilerFailure)
        {
            DotNetSourceValues.AddFailure(failures, compilerFailure);
        }

        return failures.Count == 0
            ? new DotNetKnown<DotNetSourceValue>(new DotNetCollectionValue(type, [.. values]))
            : new DotNetUnknown<DotNetSourceValue>([.. failures]);
    }

    static bool ShouldReportRootConversionFailure(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        if (expression is not CollectionExpressionSyntax { Elements.Count: > 0 } collection ||
            semanticModel.GetConversion(expression).IsUserDefined)
        {
            return true;
        }

        var errors = semanticModel.GetDiagnostics(expression.Span)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error &&
                diagnostic.Location.IsInSource &&
                diagnostic.Location.SourceTree == expression.SyntaxTree &&
                expression.Span.Contains(diagnostic.Location.SourceSpan))
            .ToArray();

        return errors.Length == 0 || errors.Any(diagnostic =>
            !collection.Elements.Any(element => element.Span.Contains(diagnostic.Location.SourceSpan)));
    }

    static bool IsDirectCollectionInitializer(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel) =>
        creation.ArgumentList?.Arguments.Count is null or 0 &&
        creation.Initializer is { Expressions.Count: > 0 } &&
        creation.Initializer.Expressions.All(initializer =>
            semanticModel.GetCollectionInitializerSymbolInfo(initializer).Symbol is IMethodSymbol method &&
            string.Equals(method.Name, "Add", StringComparison.Ordinal));

    static bool ValidateDimensions(
        ArrayCreationExpressionSyntax array,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        var outerRank = array.Type.RankSpecifiers.FirstOrDefault();
        if (outerRank is null || outerRank.Sizes.Count != 1)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                array.Type.GetLocation(),
                "Only one-dimensional outer arrays are supported by bounded collection extraction"));

            return true;
        }

        if (outerRank.Sizes[0] is not OmittedArraySizeExpressionSyntax)
        {
            var size = outerRank.Sizes[0];
            if (!TryGetExactArrayLength(size, semanticModel, out var length))
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Computed,
                    size.GetLocation(),
                    "The array dimension is not one exact non-negative constant"));
            }
            else if ((array.Initializer?.Expressions.Count ?? 0) != length)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    size.GetLocation(),
                    "The explicit array dimension does not match its authored initializer elements"));
            }
        }

        return false;
    }

    static bool TryGetExactArrayLength(
        ExpressionSyntax size,
        SemanticModel semanticModel,
        out int length)
    {
        length = default;
        var operation = semanticModel.GetOperation(size);
        var typeInfo = semanticModel.GetTypeInfo(size);
        var dimensionType = typeInfo.ConvertedType ?? operation?.Type ?? typeInfo.Type;
        if (!IsIntegralDimensionType(dimensionType) ||
            DotNetSourceValues.HasInvalidOperation(operation) ||
            !DotNetSourceValues.IsSupportedContextualConversion(semanticModel.GetConversion(size)))
        {
            return false;
        }

        var constant = operation is { ConstantValue.HasValue: true }
            ? operation.ConstantValue
            : semanticModel.GetConstantValue(size);
        if (!constant.HasValue)
        {
            return false;
        }

        return TryGetNonNegativeInt32(constant.Value, out length);
    }

    static bool IsIntegralDimensionType(ITypeSymbol? type) => type?.SpecialType is
        SpecialType.System_SByte or
        SpecialType.System_Byte or
        SpecialType.System_Int16 or
        SpecialType.System_UInt16 or
        SpecialType.System_Int32 or
        SpecialType.System_UInt32 or
        SpecialType.System_Int64 or
        SpecialType.System_UInt64 or
        SpecialType.System_Char or
        SpecialType.System_IntPtr or
        SpecialType.System_UIntPtr;

    static bool TryGetNonNegativeInt32(object? value, out int length)
    {
        switch (value)
        {
            case sbyte exact when exact >= 0:
                length = exact;
                return true;
            case byte exact:
                length = exact;
                return true;
            case short exact when exact >= 0:
                length = exact;
                return true;
            case ushort exact:
                length = exact;
                return true;
            case int exact when exact >= 0:
                length = exact;
                return true;
            case uint exact when exact <= int.MaxValue:
                length = (int)exact;
                return true;
            case long exact when exact is >= 0 and <= int.MaxValue:
                length = (int)exact;
                return true;
            case ulong exact when exact <= int.MaxValue:
                length = (int)exact;
                return true;
            case char exact:
                length = exact;
                return true;
            case IntPtr exact when exact >= 0 && exact <= int.MaxValue:
                length = (int)exact;
                return true;
            case UIntPtr exact when exact <= int.MaxValue:
                length = (int)exact;
                return true;
            default:
                length = default;
                return false;
        }
    }

    static void ValidateType(
        ExpressionSyntax expression,
        TypeInfo typeInfo,
        List<DotNetValueFailure> failures)
    {
        if (DotNetSourceValues.SemanticTypeFailure(
                expression,
                "collection type",
                true,
                typeInfo.Type,
                typeInfo.ConvertedType) is { } failure)
        {
            DotNetSourceValues.AddFailure(failures, failure);
        }
    }

    static void CollectInitializerFailures(
        InitializerExpressionSyntax initializer,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures,
        bool reportNestedShapeFailures)
    {
        foreach (var expression in initializer.Expressions)
        {
            if (expression is InitializerExpressionSyntax nested)
            {
                if (reportNestedShapeFailures)
                {
                    DotNetSourceValues.AddFailure(failures, new(
                        DotNetValueFailureKind.Unsupported,
                        nested.GetLocation(),
                        "A nested collection initializer is outside bounded extraction"));
                }

                CollectInitializerFailures(nested, semanticModel, failures, reportNestedShapeFailures);
            }
            else if (DotNetSourceValues.Extract(expression, semanticModel) is DotNetUnknown<DotNetSourceValue> unknown)
            {
                DotNetSourceValues.AddFailures(failures, unknown.Failures);
            }
        }
    }

    static void CollectSpreadFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        if (DotNetSourceValues.Extract(expression, semanticModel) is not DotNetUnknown<DotNetSourceValue> unknown)
        {
            return;
        }

        DotNetSourceValues.AddFailures(failures, unknown.Failures);
    }
}
