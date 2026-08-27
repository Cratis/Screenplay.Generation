// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Screenplay.Generation.DotNet;

internal static class DotNetPayloadValues
{
    public static DotNetBounded<DotNetSourceValue> Extract(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel)
    {
        var failures = new List<DotNetValueFailure>();
        var operation = ObjectCreationOperationFor(semanticModel.GetOperation(creation));
        var typeInfo = semanticModel.GetTypeInfo(creation);
        var typeFailure = DotNetSourceValues.SemanticTypeFailure(
            creation,
            "payload type",
            true,
            typeInfo.Type,
            typeInfo.ConvertedType,
            operation?.Type);
        if (operation?.Constructor is not { } constructor || operation.Type is null || typeFailure is not null)
        {
            var payloadType = typeFailure is null
                ? typeInfo.Type ?? typeInfo.ConvertedType ?? operation?.Type
                : null;
            var bindingFailure = payloadType is INamedTypeSymbol { IsAbstract: true } &&
                DotNetSourceValues.ConstructionRootFailure(creation, semanticModel) is { } abstractRootFailure
                    ? abstractRootFailure
                    : new DotNetValueFailure(
                        DotNetValueFailureKind.Unbound,
                        creation.GetLocation(),
                        "The payload construction has no exact bound constructor and type");
            DotNetSourceValues.AddFailure(failures, typeFailure ?? bindingFailure);
            CollectAuthoredValues(creation, payloadType, semanticModel, failures);

            return new DotNetUnknown<DotNetSourceValue>([.. failures]);
        }

        if (DotNetSourceValues.ConstructionRootFailure(creation, semanticModel) is { } rootFailure)
        {
            DotNetSourceValues.AddFailure(failures, rootFailure);
        }

        if (DotNetSourceValues.ContextualConversionFailure(
                creation,
                semanticModel,
                "payload value",
                false) is { } conversionFailure)
        {
            DotNetSourceValues.AddFailure(failures, conversionFailure);
        }

        var explicitArguments = operation.Arguments
            .Where(argument => !argument.IsImplicit && argument.Syntax is ArgumentSyntax)
            .ToArray();
        var hasExactConstructorShape =
            explicitArguments.Length == constructor.Parameters.Length &&
            constructor.Parameters.All(parameter =>
                explicitArguments.Count(argument => SymbolEqualityComparer.Default.Equals(argument.Parameter, parameter)) == 1) &&
            explicitArguments.All(argument => argument.Parameter is not null && argument.ArgumentKind != ArgumentKind.ParamArray);

        var values = new List<DotNetNamedValue>();
        var symbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        if (!hasExactConstructorShape)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                creation.ArgumentList?.GetLocation() ?? creation.GetLocation(),
                "Every payload constructor parameter must be stated by one exact authored argument"));

            foreach (var argument in creation.ArgumentList?.Arguments ?? [])
            {
                CollectValueFailures(argument.Expression, semanticModel, failures, true);
            }
        }
        else
        {
            var constructorValues = new List<DotNetNamedValue>();
            foreach (var authoredArgument in creation.ArgumentList?.Arguments ?? [])
            {
                var argument = explicitArguments.Single(argument => argument.Syntax.Span == authoredArgument.Span);
                var parameter = argument.Parameter!;
                Add(
                    parameter.Name,
                    parameter,
                    authoredArgument.Expression,
                    semanticModel,
                    symbols,
                    constructorValues,
                    failures);
            }

            values.AddRange(constructorValues.OrderBy(value => ((IParameterSymbol)value.Symbol).Ordinal));
        }

        CollectInitializers(creation, operation.Type, semanticModel, symbols, values, failures);
        if (failures.Count == 0 &&
            DotNetSourceValues.CompilerErrorFailure(creation, semanticModel, "payload") is { } compilerFailure)
        {
            DotNetSourceValues.AddFailure(failures, compilerFailure);
        }

        return failures.Count == 0
            ? new DotNetKnown<DotNetSourceValue>(new DotNetPayloadValue(operation.Type, [.. values]))
            : new DotNetUnknown<DotNetSourceValue>([.. failures]);
    }

    static ISymbol? DirectAssignableMemberFor(
        AssignmentExpressionSyntax assignment,
        ITypeSymbol payloadType,
        SemanticModel semanticModel)
    {
        if (assignment.Left is not IdentifierNameSyntax)
        {
            return null;
        }

        var target = semanticModel.GetOperation(assignment) is ISimpleAssignmentOperation assignmentOperation
            ? assignmentOperation.Target
            : semanticModel.GetOperation(assignment.Left);
        if (DotNetSourceValues.HasInvalidOperation(target))
        {
            return null;
        }

        ISymbol? member = target switch
        {
            IPropertyReferenceOperation property => property.Property,
            IFieldReferenceOperation field => field.Field,
            _ => null
        };
        if (!IsDirectMember(member, payloadType) || !IsAssignable(member!, assignment.SpanStart, semanticModel))
        {
            return null;
        }

        return member;
    }

    static bool IsDirectMember(ISymbol? member, ITypeSymbol payloadType) => member switch
    {
        IPropertySymbol { IsIndexer: false, IsStatic: false } property => IsConstructedTypeOrBaseType(property.ContainingType, payloadType),
        IFieldSymbol { IsStatic: false } field => IsConstructedTypeOrBaseType(field.ContainingType, payloadType),
        _ => false
    };

    static bool IsConstructedTypeOrBaseType(ITypeSymbol memberType, ITypeSymbol payloadType)
    {
        for (var type = payloadType as INamedTypeSymbol; type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(memberType, type))
            {
                return true;
            }
        }

        return false;
    }

    static bool IsAssignable(ISymbol member, int position, SemanticModel semanticModel) => member switch
    {
        IPropertySymbol { SetMethod: { } setter } => semanticModel.IsAccessible(position, setter),
        IFieldSymbol { IsReadOnly: false, IsConst: false } field => semanticModel.IsAccessible(position, field),
        _ => false
    };

    static void CollectAuthoredValues(
        BaseObjectCreationExpressionSyntax creation,
        ITypeSymbol? payloadType,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        foreach (var argument in creation.ArgumentList?.Arguments ?? [])
        {
            CollectFailures(argument.Expression, semanticModel, failures);
        }

        CollectInitializers(creation, payloadType, semanticModel, [], [], failures);
    }

    static void CollectInitializers(
        BaseObjectCreationExpressionSyntax creation,
        ITypeSymbol? payloadType,
        SemanticModel semanticModel,
        HashSet<ISymbol> symbols,
        List<DotNetNamedValue> values,
        List<DotNetValueFailure> failures)
    {
        foreach (var initializer in creation.Initializer?.Expressions ?? [])
        {
            if (initializer is not AssignmentExpressionSyntax assignment)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    initializer.GetLocation(),
                    "A payload initializer contains a non-assignment element"));
                CollectFailures(initializer, semanticModel, failures);
                continue;
            }

            if (assignment.Left is ImplicitElementAccessSyntax index)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    index.GetLocation(),
                    "The payload initializer is not a direct member assignment"));
                foreach (var argument in index.ArgumentList.Arguments)
                {
                    CollectFailures(argument.Expression, semanticModel, failures);
                }

                CollectValueFailures(
                    assignment.Right,
                    semanticModel,
                    failures,
                    assignment.Right is not InitializerExpressionSyntax);
                continue;
            }

            var member = payloadType is null
                ? null
                : DirectAssignableMemberFor(assignment, payloadType, semanticModel);
            if (assignment.Right is InitializerExpressionSyntax || member is null)
            {
                DotNetSourceValues.AddFailure(failures, new(
                    DotNetValueFailureKind.Unsupported,
                    assignment.Left.GetLocation(),
                    "The payload initializer is not a direct member assignment"));
                CollectValueFailures(
                    assignment.Right,
                    semanticModel,
                    failures,
                    assignment.Right is not InitializerExpressionSyntax);
                continue;
            }

            Add(member.Name, member, assignment.Right, semanticModel, symbols, values, failures);
        }
    }

    static void CollectValueFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures,
        bool validateSemanticConversion,
        ITypeSymbol? expectedType = null)
    {
        if (validateSemanticConversion && HasInvalidValueOperation(expression, semanticModel, expectedType))
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                expression.GetLocation(),
                "The payload value conversion is not semantically valid"));
        }

        CollectFailures(expression, semanticModel, failures);
    }

    static void CollectFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        if (expression is InitializerExpressionSyntax initializer)
        {
            foreach (var child in initializer.Expressions)
            {
                if (child is AssignmentExpressionSyntax assignment)
                {
                    CollectIndexKeyFailures(assignment.Left, semanticModel, failures);
                    CollectFailures(assignment.Right, semanticModel, failures);
                }
                else
                {
                    CollectFailures(child, semanticModel, failures);
                }
            }

            return;
        }

        if (DotNetSourceValues.Extract(expression, semanticModel) is DotNetUnknown<DotNetSourceValue> unknown)
        {
            DotNetSourceValues.AddFailures(failures, unknown.Failures);
        }
    }

    static void CollectIndexKeyFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
        var arguments = expression switch
        {
            ImplicitElementAccessSyntax implicitElement => implicitElement.ArgumentList.Arguments,
            ElementAccessExpressionSyntax element => element.ArgumentList.Arguments,
            _ => default
        };
        foreach (var argument in arguments)
        {
            CollectFailures(argument.Expression, semanticModel, failures);
        }
    }

    static void Add(
        string name,
        ISymbol symbol,
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        HashSet<ISymbol> symbols,
        List<DotNetNamedValue> values,
        List<DotNetValueFailure> failures)
    {
        var isDuplicate = !symbols.Add(symbol);
        if (isDuplicate)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.DuplicateMember,
                expression.GetLocation(),
                $"Payload member '{name}' is assigned more than once"));
        }

        var expectedType = symbol switch
        {
            IParameterSymbol parameter => parameter.Type,
            IPropertySymbol property => property.Type,
            IFieldSymbol field => field.Type,
            _ => null
        };
        var hasInvalidValueOperation = HasInvalidValueOperation(expression, semanticModel, expectedType);
        if (hasInvalidValueOperation)
        {
            DotNetSourceValues.AddFailure(failures, new(
                DotNetValueFailureKind.Unsupported,
                expression.GetLocation(),
                "The payload value conversion is not semantically valid"));
        }

        switch (DotNetSourceValues.Extract(expression, semanticModel))
        {
            case DotNetKnown<DotNetSourceValue> known when !isDuplicate && !hasInvalidValueOperation:
                values.Add(new(name, symbol, known.Value, expression.GetLocation()));
                break;
            case DotNetUnknown<DotNetSourceValue> unknown:
                DotNetSourceValues.AddFailures(failures, unknown.Failures);
                break;
        }
    }

    static bool HasInvalidValueOperation(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol? expectedType = null)
    {
        var convertedType = semanticModel.GetTypeInfo(expression).ConvertedType;
        return (expectedType is not null &&
                !DotNetSourceValues.IsSupportedContextualConversion(semanticModel.ClassifyConversion(expression, expectedType))) ||
            (convertedType is not null &&
                !DotNetSourceValues.IsSupportedContextualConversion(semanticModel.ClassifyConversion(expression, convertedType))) ||
            DotNetSourceValues.HasInvalidOperation(semanticModel.GetOperation(expression));
    }

    static IObjectCreationOperation? ObjectCreationOperationFor(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation as IObjectCreationOperation;
    }
}
