// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Screenplay.Generation.DotNet;

internal static class DotNetPayloadValues
{
    const string ConversionFailureMessage = "The payload value conversion is not semantically valid";

    public static DotNetBounded<DotNetSourceValue> Extract(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel)
    {
        var operation = ObjectCreationOperationFor(semanticModel.GetOperation(creation));
        var typeInfo = semanticModel.GetTypeInfo(creation);
        var typeFailure = DotNetSourceValues.SemanticTypeFailure(
            creation,
            "payload type",
            true,
            typeInfo.Type,
            typeInfo.ConvertedType,
            operation?.Type);
        return operation?.Constructor is { } constructor && operation.Type is not null && typeFailure is null
            ? ExtractBound(creation, operation, constructor, semanticModel)
            : ExtractUnbound(
                creation,
                typeFailure is null ? typeInfo.Type ?? typeInfo.ConvertedType ?? operation?.Type : null,
                typeFailure,
                semanticModel);
    }

    static DotNetUnknown<DotNetSourceValue> ExtractUnbound(
        BaseObjectCreationExpressionSyntax creation,
        ITypeSymbol? payloadType,
        DotNetValueFailure? typeFailure,
        SemanticModel semanticModel)
    {
        var state = new PayloadState(semanticModel);
        DotNetSourceValues.AddFailure(state.Failures, typeFailure ?? BindingFailure(creation, payloadType, semanticModel));
        foreach (var argument in creation.ArgumentList?.Arguments ?? [])
        {
            CollectFailures(argument.Expression, semanticModel, state.Failures);
        }

        CollectInitializers(creation, payloadType, state);
        return new DotNetUnknown<DotNetSourceValue>([.. state.Failures]);
    }

    static DotNetValueFailure BindingFailure(
        BaseObjectCreationExpressionSyntax creation,
        ITypeSymbol? payloadType,
        SemanticModel semanticModel) =>
        payloadType is INamedTypeSymbol { IsAbstract: true } &&
        DotNetSourceValues.ConstructionRootFailure(creation, semanticModel) is { } abstractRootFailure
            ? abstractRootFailure
            : new DotNetValueFailure(
                DotNetValueFailureKind.Unbound,
                creation.GetLocation(),
                "The payload construction has no exact bound constructor and type");

    static DotNetBounded<DotNetSourceValue> ExtractBound(
        BaseObjectCreationExpressionSyntax creation,
        IObjectCreationOperation operation,
        IMethodSymbol constructor,
        SemanticModel semanticModel)
    {
        var state = new PayloadState(semanticModel);
        AddCreationFailures(creation, semanticModel, state.Failures);

        var authoredArguments = creation.ArgumentList?.Arguments ?? [];
        var explicitArguments = operation.Arguments
            .Where(argument => authoredArguments.Any(authored => authored.Span.Contains(argument.Syntax.Span)))
            .ToArray();
        if (HasExactConstructorShape(explicitArguments, constructor))
        {
            CollectConstructorArguments(authoredArguments, explicitArguments, state);
        }
        else
        {
            CollectInexactArguments(creation, state);
        }

        CollectInitializers(creation, operation.Type, state);
        if (state.Failures.Count == 0 &&
            DotNetSourceValues.CompilerErrorFailure(creation, semanticModel, "payload") is { } compilerFailure)
        {
            DotNetSourceValues.AddFailure(state.Failures, compilerFailure);
        }

        return state.Failures.Count == 0
            ? new DotNetKnown<DotNetSourceValue>(new DotNetPayloadValue(operation.Type!, [.. state.Values]))
            : new DotNetUnknown<DotNetSourceValue>([.. state.Failures]);
    }

    static void AddCreationFailures(
        BaseObjectCreationExpressionSyntax creation,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
    {
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
    }

    static bool HasExactConstructorShape(IArgumentOperation[] explicitArguments, IMethodSymbol constructor) =>
        explicitArguments.Length == constructor.Parameters.Length &&
        constructor.Parameters.All(parameter =>
            explicitArguments.Count(argument => SymbolEqualityComparer.Default.Equals(argument.Parameter, parameter)) == 1) &&
        explicitArguments.All(argument => argument.Parameter is not null && argument.ArgumentKind != ArgumentKind.ParamArray);

    static void CollectInexactArguments(BaseObjectCreationExpressionSyntax creation, PayloadState state)
    {
        DotNetSourceValues.AddFailure(state.Failures, new(
            DotNetValueFailureKind.Unsupported,
            creation.ArgumentList?.GetLocation() ?? creation.GetLocation(),
            "Every payload constructor parameter must be stated by one exact authored argument"));

        foreach (var argument in creation.ArgumentList?.Arguments ?? [])
        {
            CollectValueFailures(argument.Expression, state.SemanticModel, state.Failures, true);
        }
    }

    static void CollectConstructorArguments(
        SeparatedSyntaxList<ArgumentSyntax> authoredArguments,
        IArgumentOperation[] explicitArguments,
        PayloadState state)
    {
        foreach (var authoredArgument in authoredArguments)
        {
            var argument = explicitArguments.Single(argument => authoredArgument.Span.Contains(argument.Syntax.Span));
            var parameter = argument.Parameter!;
            Add(parameter.Name, parameter, authoredArgument.Expression, state);
        }

        var ordered = state.Values.OrderBy(value => ((IParameterSymbol)value.Symbol).Ordinal).ToArray();
        state.Values.Clear();
        state.Values.AddRange(ordered);
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

        var member = ReferencedMember(target);
        return IsDirectMember(member, payloadType) && IsAssignable(member!, assignment.SpanStart, semanticModel)
            ? member
            : null;
    }

    static ISymbol? ReferencedMember(IOperation? target) => target switch
    {
        IPropertyReferenceOperation property => property.Property,
        IFieldReferenceOperation field => field.Field,
        _ => null
    };

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

    static void CollectInitializers(
        BaseObjectCreationExpressionSyntax creation,
        ITypeSymbol? payloadType,
        PayloadState state)
    {
        foreach (var initializer in creation.Initializer?.Expressions ?? [])
        {
            CollectInitializer(initializer, payloadType, state);
        }
    }

    static void CollectInitializer(ExpressionSyntax initializer, ITypeSymbol? payloadType, PayloadState state)
    {
        var semanticModel = state.SemanticModel;
        if (initializer is not AssignmentExpressionSyntax assignment)
        {
            AddNotDirectFailure(state, initializer, "A payload initializer contains a non-assignment element");
            CollectFailures(initializer, semanticModel, state.Failures);
            return;
        }

        if (assignment.Left is ImplicitElementAccessSyntax index)
        {
            AddNotDirectFailure(state, index, "The payload initializer is not a direct member assignment");
            foreach (var argument in index.ArgumentList.Arguments)
            {
                CollectFailures(argument.Expression, semanticModel, state.Failures);
            }

            CollectAssignedValueFailures(assignment, state);
            return;
        }

        var member = payloadType is null
            ? null
            : DirectAssignableMemberFor(assignment, payloadType, semanticModel);
        if (assignment.Right is InitializerExpressionSyntax || member is null)
        {
            AddNotDirectFailure(state, assignment.Left, "The payload initializer is not a direct member assignment");
            CollectAssignedValueFailures(assignment, state);
            return;
        }

        Add(member.Name, member, assignment.Right, state);
    }

    static void AddNotDirectFailure(PayloadState state, SyntaxNode source, string message) =>
        DotNetSourceValues.AddFailure(state.Failures, new(
            DotNetValueFailureKind.Unsupported,
            source.GetLocation(),
            message));

    static void CollectAssignedValueFailures(AssignmentExpressionSyntax assignment, PayloadState state) =>
        CollectValueFailures(
            assignment.Right,
            state.SemanticModel,
            state.Failures,
            assignment.Right is not InitializerExpressionSyntax);

    static void CollectValueFailures(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures,
        bool validateSemanticConversion)
    {
        if (validateSemanticConversion && HasInvalidValueOperation(expression, semanticModel))
        {
            AddConversionFailure(failures, expression);
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
                CollectChildFailures(child, semanticModel, failures);
            }

            return;
        }

        if (DotNetSourceValues.Extract(expression, semanticModel) is DotNetUnknown<DotNetSourceValue> unknown)
        {
            DotNetSourceValues.AddFailures(failures, unknown.Failures);
        }
    }

    static void CollectChildFailures(
        ExpressionSyntax child,
        SemanticModel semanticModel,
        List<DotNetValueFailure> failures)
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

    static void AddConversionFailure(List<DotNetValueFailure> failures, ExpressionSyntax expression) =>
        DotNetSourceValues.AddFailure(failures, new(
            DotNetValueFailureKind.Unsupported,
            expression.GetLocation(),
            ConversionFailureMessage));

    static void Add(string name, ISymbol symbol, ExpressionSyntax expression, PayloadState state)
    {
        var semanticModel = state.SemanticModel;
        var isDuplicate = state.Symbols.Exists(existing => SymbolEqualityComparer.Default.Equals(existing, symbol));
        if (!isDuplicate)
        {
            state.Symbols.Add(symbol);
        }

        if (isDuplicate)
        {
            DotNetSourceValues.AddFailure(state.Failures, new(
                DotNetValueFailureKind.DuplicateMember,
                expression.GetLocation(),
                $"Payload member '{name}' is assigned more than once"));
        }

        var hasInvalidValueConversion = HasInvalidValueConversion(expression, semanticModel, ExpectedTypeOf(symbol));
        if (hasInvalidValueConversion)
        {
            AddConversionFailure(state.Failures, expression);
        }

        var extracted = DotNetSourceValues.Extract(expression, semanticModel);
        if (extracted is DotNetUnknown<DotNetSourceValue> unknown)
        {
            DotNetSourceValues.AddFailures(state.Failures, unknown.Failures);
            return;
        }

        if (DotNetSourceValues.HasInvalidOperation(semanticModel.GetOperation(expression)))
        {
            AddConversionFailure(state.Failures, expression);
        }
        else if (!isDuplicate && !hasInvalidValueConversion)
        {
            state.Values.Add(new(name, symbol, ((DotNetKnown<DotNetSourceValue>)extracted).Value, expression.GetLocation()));
        }
    }

    static ITypeSymbol? ExpectedTypeOf(ISymbol symbol) => symbol switch
    {
        IParameterSymbol parameter => parameter.Type,
        IPropertySymbol property => property.Type,
        IFieldSymbol field => field.Type,
        _ => null
    };

    static bool HasInvalidValueConversion(
        ExpressionSyntax expression,
        SemanticModel semanticModel,
        ITypeSymbol? expectedType = null)
    {
        var convertedType = semanticModel.GetTypeInfo(expression).ConvertedType;
        return IsUnsupportedConversionTo(expression, semanticModel, expectedType) ||
            IsUnsupportedConversionTo(expression, semanticModel, convertedType);
    }

    static bool IsUnsupportedConversionTo(ExpressionSyntax expression, SemanticModel semanticModel, ITypeSymbol? type) =>
        type is not null &&
        !DotNetSourceValues.IsSupportedContextualConversion(semanticModel.ClassifyConversion(expression, type));

    static bool HasInvalidValueOperation(ExpressionSyntax expression, SemanticModel semanticModel) =>
        HasInvalidValueConversion(expression, semanticModel) ||
        DotNetSourceValues.HasInvalidOperation(semanticModel.GetOperation(expression));

    static IObjectCreationOperation? ObjectCreationOperationFor(IOperation? operation)
    {
        while (operation is IConversionOperation conversion)
        {
            operation = conversion.Operand;
        }

        return operation as IObjectCreationOperation;
    }

    sealed class PayloadState(SemanticModel semanticModel)
    {
        public SemanticModel SemanticModel { get; } = semanticModel;

        public List<ISymbol> Symbols { get; } = [];

        public List<DotNetNamedValue> Values { get; } = [];

        public List<DotNetValueFailure> Failures { get; } = [];
    }
}
