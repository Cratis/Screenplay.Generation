// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Provides fail-closed semantic helpers for C# invocation expressions.
/// </summary>
public static class DotNetInvocations
{
    /// <summary>
    /// Gets the exactly bound method for an invocation.
    /// </summary>
    /// <param name="invocation">The invocation to resolve.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The bound method, or <see langword="null"/> when binding is missing or ambiguous.</returns>
    public static IMethodSymbol? MethodFor(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel) =>
        semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;

    /// <summary>
    /// Gets the original method definition behind a direct or reduced extension invocation.
    /// </summary>
    /// <param name="method">The bound invocation method.</param>
    /// <returns>The original non-constructed method definition.</returns>
    public static IMethodSymbol DefinitionOf(IMethodSymbol method) =>
        (method.ReducedFrom ?? method).OriginalDefinition;

    /// <summary>
    /// Gets the argument bound to one formal parameter.
    /// </summary>
    /// <param name="invocation">The invocation containing the argument.</param>
    /// <param name="method">The method symbol bound to <paramref name="invocation"/>.</param>
    /// <param name="parameterName">The exact formal parameter name.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The exactly bound authored argument, or <see langword="null"/> when it is omitted, expanded, or unresolved.</returns>
    public static ArgumentSyntax? ArgumentForParameter(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        string parameterName,
        SemanticModel semanticModel)
    {
        var definition = DefinitionOf(method);
        var parameterExists =
            method.Parameters.Any(_ => string.Equals(_.Name, parameterName, StringComparison.Ordinal)) ||
            (definition.IsExtensionMethod &&
             definition.Parameters.FirstOrDefault() is { } receiver &&
             string.Equals(receiver.Name, parameterName, StringComparison.Ordinal));
        if (!parameterExists || semanticModel.GetOperation(invocation) is not IInvocationOperation operation)
        {
            return null;
        }

        var arguments = operation.Arguments
            .Where(_ => string.Equals(_.Parameter?.Name, parameterName, StringComparison.Ordinal) &&
                        !_.IsImplicit &&
                        _.Syntax is ArgumentSyntax)
            .Select(_ => (ArgumentSyntax)_.Syntax)
            .ToArray();
        return arguments.Length == 1 ? arguments[0] : null;
    }

    /// <summary>
    /// Gets the semantic receiver expression for an instance, reduced extension, or static extension invocation.
    /// </summary>
    /// <param name="invocation">The invocation to inspect.</param>
    /// <param name="method">The method symbol bound to <paramref name="invocation"/>.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The receiver expression, or <see langword="null"/> when no receiver expression is authored.</returns>
    public static ExpressionSyntax? ReceiverFor(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel)
    {
        if (method.ReducedFrom is not null)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax member => member.Expression,
                MemberBindingExpressionSyntax => invocation.Parent is ConditionalAccessExpressionSyntax conditional
                    ? conditional.Expression
                    : null,
                _ => null
            };
        }

        var definition = DefinitionOf(method);
        if (definition.IsExtensionMethod && definition.Parameters.FirstOrDefault() is { } receiver)
        {
            return ArgumentForParameter(invocation, method, receiver.Name, semanticModel)?.Expression;
        }

        if (method.IsStatic)
        {
            return null;
        }

        return invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Expression,
            MemberBindingExpressionSyntax => invocation.Parent is ConditionalAccessExpressionSyntax conditional
                ? conditional.Expression
                : null,
            _ => null
        };
    }

    /// <summary>
    /// Gets the handler or method parameter at the root of an invocation receiver expression.
    /// </summary>
    /// <param name="invocation">The invocation to inspect.</param>
    /// <param name="method">The method symbol bound to <paramref name="invocation"/>.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The root parameter, or <see langword="null"/> when the receiver is not directly rooted in one parameter.</returns>
    public static IParameterSymbol? ReceiverRootParameter(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method,
        SemanticModel semanticModel) =>
        ReceiverFor(invocation, method, semanticModel) is { } receiver
            ? RootParameter(receiver, semanticModel)
            : null;

    /// <summary>
    /// Gets the parameter at the root of a bounded receiver/member expression.
    /// </summary>
    /// <param name="expression">The expression to inspect.</param>
    /// <param name="semanticModel">The owning semantic model.</param>
    /// <returns>The root parameter, or <see langword="null"/> when a local, invocation result, dynamic value, or another unsupported shape intervenes.</returns>
    public static IParameterSymbol? RootParameter(
        ExpressionSyntax expression,
        SemanticModel semanticModel)
    {
        while (true)
        {
            if (semanticModel.GetSymbolInfo(expression).Symbol is IParameterSymbol parameter)
            {
                return parameter;
            }

            expression = expression switch
            {
                ParenthesizedExpressionSyntax parenthesized => parenthesized.Expression,
                CastExpressionSyntax cast => cast.Expression,
                MemberAccessExpressionSyntax member => member.Expression,
                MemberBindingExpressionSyntax binding => binding.Ancestors()
                    .OfType<ConditionalAccessExpressionSyntax>()
                    .FirstOrDefault()?.Expression!,
                ElementAccessExpressionSyntax element => element.Expression,
                ConditionalAccessExpressionSyntax conditional => conditional.Expression,
                _ => null!
            };

            if (expression is null)
            {
                return null;
            }
        }
    }
}
