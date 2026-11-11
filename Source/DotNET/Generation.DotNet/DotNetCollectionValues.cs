// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
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
        var elements = ElementsOf(expression, semanticModel);
        if (elements is null)
        {
            return null;
        }

        var state = new CollectionState(expression, semanticModel);
        state.HasUnsupportedRank = ValidateRank(expression, semanticModel, state.Failures);
        var typeInfo = semanticModel.GetTypeInfo(expression);
        var type = typeInfo.Type ?? typeInfo.ConvertedType;
        ValidateType(expression, typeInfo, state.Failures);
        AddRootFailures(expression, semanticModel, state.Failures);

        foreach (var element in elements)
        {
            CollectElement(element, state);
        }

        if (state.Failures.Count == 0 &&
            DotNetSourceValues.CompilerErrorFailure(expression, semanticModel, "collection") is { } compilerFailure)
        {
            DotNetSourceValues.AddFailure(state.Failures, compilerFailure);
        }

        return state.Failures.Count == 0
            ? new DotNetKnown<DotNetSourceValue>(new DotNetCollectionValue(type, [.. state.Values]))
            : new DotNetUnknown<DotNetSourceValue>([.. state.Failures]);
    }

    static IReadOnlyList<SyntaxNode>? ElementsOf(ExpressionSyntax expression, SemanticModel semanticModel) => expression switch
    {
        ArrayCreationExpressionSyntax collectionArray => collectionArray.Initializer?.Expressions ?? [],
        ImplicitArrayCreationExpressionSyntax { Initializer: { } initializer } => initializer.Expressions,
        InitializerExpressionSyntax initializer => initializer.Expressions,
        CollectionExpressionSyntax collection => collection.Elements,
        BaseObjectCreationExpressionSyntax { Initializer: { } initializer } collectionCreation
            when IsDirectCollectionInitializer(collectionCreation, semanticModel) => initializer.Expressions,
        _ => null
    };

    static bool ValidateRank(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        if (expression is ArrayCreationExpressionSyntax array)
        {
            if (array.Initializer is null)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    array.GetLocation(),
                    "An array without an initializer has no individually authored elements"));
            }

            return ValidateDimensions(array, semanticModel, failures);
        }

        if (expression is ImplicitArrayCreationExpressionSyntax { Commas.Count: > 0 } implicitMultidimensional)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                implicitMultidimensional.GetLocation(),
                "Only one-dimensional arrays are supported by bounded collection extraction"));
            return true;
        }

        return false;
    }

    static void AddRootFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        if (RootConversionFailure(expression, semanticModel) is { } conversionFailure)
        {
            DotNetSourceValues.AddFailure(failures, conversionFailure);
        }

        if (expression is BaseObjectCreationExpressionSyntax creation &&
            DotNetSourceValues.ConstructionRootFailure(creation, semanticModel) is { } rootFailure)
        {
            DotNetSourceValues.AddFailure(failures, rootFailure);
        }
    }

    static DotNetValueFailure? RootConversionFailure(ExpressionSyntax expression, SemanticModel semanticModel) =>
        expression is not InitializerExpressionSyntax &&
        DotNetSourceValues.ContextualConversionFailure(expression, semanticModel, "collection value", false) is { } failure &&
        ShouldReportRootConversionFailure(expression, semanticModel)
            ? failure
            : null;

    static ExpressionSyntax? ElementExpressionOf(SyntaxNode element) => element switch
    {
        ExpressionElementSyntax expressionElement => expressionElement.Expression,
        ExpressionSyntax direct => direct,
        _ => null
    };

    static void CollectElement(SyntaxNode element, CollectionState state)
    {
        if (element is SpreadElementSyntax spread)
        {
            DotNetSourceValues.AddFailure(state.Failures, new(
                DotNetValueFailureKind.OpaqueSpread,
                spread.GetLocation(),
                "A collection spread hides elements that are not authored individually"));
            CollectSpreadFailures(spread.Expression, state.SemanticModel, state.Failures);
            return;
        }

        var elementExpression = ElementExpressionOf(element);
        if (elementExpression is null)
        {
            DotNetSourceValues.AddFailure(state.Failures, new(
                DotNetValueFailureKind.Unsupported,
                element.GetLocation(),
                "The collection element shape is outside bounded extraction"));
            return;
        }

        if (elementExpression is InitializerExpressionSyntax nestedInitializer)
        {
            if (!state.IsDirectEntry(nestedInitializer))
            {
                ReportNestedInitializer(nestedInitializer, state);
                return;
            }

            elementExpression = nestedInitializer.Expressions[0];
        }

        AddExtractedElement(elementExpression, state);
    }

    static void ReportNestedInitializer(InitializerExpressionSyntax nestedInitializer, CollectionState state)
    {
        if (!state.HasUnsupportedRank)
        {
            DotNetSourceValues.AddFailure(state.Failures, new(
                DotNetValueFailureKind.Unsupported,
                nestedInitializer.GetLocation(),
                state.Expression is BaseObjectCreationExpressionSyntax
                    ? "A direct collection initializer entry must contain one authored value"
                    : "A nested collection initializer is outside bounded extraction"));
        }

        CollectInitializerFailures(
            nestedInitializer,
            state.SemanticModel,
            state.Failures,
            !state.HasUnsupportedRank);
    }

    static void AddExtractedElement(ExpressionSyntax elementExpression, CollectionState state)
    {
        var extracted = DotNetSourceValues.Extract(elementExpression, state.SemanticModel);
        if (extracted is DotNetUnknown<DotNetSourceValue> unknown)
        {
            DotNetSourceValues.AddFailures(state.Failures, unknown.Failures);
            return;
        }

        if (DotNetSourceValues.ContextualConversionFailure(
                elementExpression,
                state.SemanticModel,
                "collection element") is { } elementConversionFailure)
        {
            DotNetSourceValues.AddFailure(state.Failures, elementConversionFailure);
        }
        else if (extracted is DotNetKnown<DotNetSourceValue> known)
        {
            state.Values.Add(new(known.Value, elementExpression.GetLocation()));
        }
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
            ValidateDimension(array, outerRank.Sizes[0], semanticModel, failures);
        }

        return false;
    }

    static void ValidateDimension(
        ArrayCreationExpressionSyntax array,
        ExpressionSyntax size,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        if (DotNetSourceValues.ConditionalExpressionWithin(size) is { } conditionalDimension)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Conditional,
                conditionalDimension.GetLocation(),
                "The array dimension depends on a condition"));
        }
        else if (!TryGetExactArrayLength(size, semanticModel, out var length))
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

    static bool TryGetExactArrayLength(
        ExpressionSyntax size,
        SemanticModel semanticModel,
        out int length)
    {
        length = default;
        var operation = semanticModel.GetOperation(size);
        var typeInfo = semanticModel.GetTypeInfo(size);
        var dimensionType = typeInfo.ConvertedType ?? operation?.Type ?? typeInfo.Type;
        if (!IsExactDimension(size, semanticModel, operation, dimensionType))
        {
            return false;
        }

        var constant = operation is { ConstantValue.HasValue: true }
            ? operation.ConstantValue
            : semanticModel.GetConstantValue(size);
        return constant.HasValue && TryGetNonNegativeInt32(constant.Value, out length);
    }

    static bool IsExactDimension(
        ExpressionSyntax size,
        SemanticModel semanticModel,
        Microsoft.CodeAnalysis.IOperation? operation,
        ITypeSymbol? dimensionType) =>
        IsIntegralDimensionType(dimensionType) &&
        !DotNetSourceValues.HasInvalidOperation(operation) &&
        DotNetSourceValues.IsSupportedContextualConversion(semanticModel.GetConversion(size));

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
        var exact = value switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong =>
                Convert.ToDecimal(value, CultureInfo.InvariantCulture),
            char typed => typed,
            IntPtr typed => (long)typed,
            UIntPtr typed => (ulong)typed,
            _ => (decimal?)null
        };
        var isLength = exact is >= 0 and <= int.MaxValue;
        length = isLength ? (int)exact!.Value : default;
        return isLength;
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

    sealed class CollectionState(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        public ExpressionSyntax Expression { get; } = expression;

        public SemanticModel SemanticModel { get; } = semanticModel;

        public bool HasUnsupportedRank { get; set; }

        public List<DotNetValueFailure> Failures { get; } = [];

        public List<DotNetCollectionElement> Values { get; } = [];

        public bool IsDirectEntry(InitializerExpressionSyntax nestedInitializer) =>
            Expression is BaseObjectCreationExpressionSyntax && nestedInitializer.Expressions.Count == 1;
    }
}
