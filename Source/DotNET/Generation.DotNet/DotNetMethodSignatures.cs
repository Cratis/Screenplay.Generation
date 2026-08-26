// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Describes one exact .NET method parameter independently from invocation syntax.
/// </summary>
public sealed record DotNetParameterSignature
{
    /// <summary>
    /// Gets the exact parameter type.
    /// </summary>
    public required ITypeSymbol Type { get; init; }

    /// <summary>
    /// Gets the parameter reference kind.
    /// </summary>
    public required RefKind RefKind { get; init; }

    /// <summary>
    /// Gets whether the parameter is a <c>params</c> collection.
    /// </summary>
    public required bool IsParams { get; init; }

    /// <summary>
    /// Gets whether the parameter is the extension receiver.
    /// </summary>
    public required bool IsExtensionReceiver { get; init; }
}

/// <summary>
/// Describes one exact normalized .NET method signature.
/// </summary>
public sealed record DotNetMethodSignature
{
    /// <summary>
    /// Gets the exact original containing type.
    /// </summary>
    public required INamedTypeSymbol ContainingType { get; init; }

    /// <summary>
    /// Gets the exact method name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the Roslyn method kind.
    /// </summary>
    public required MethodKind MethodKind { get; init; }

    /// <summary>
    /// Gets whether the method is static.
    /// </summary>
    public required bool IsStatic { get; init; }

    /// <summary>
    /// Gets whether the method is an extension method.
    /// </summary>
    public required bool IsExtensionMethod { get; init; }

    /// <summary>
    /// Gets the generic arity.
    /// </summary>
    public required int GenericArity { get; init; }

    /// <summary>
    /// Gets the exact return type.
    /// </summary>
    public required ITypeSymbol ReturnType { get; init; }

    /// <summary>
    /// Gets the return reference kind.
    /// </summary>
    public required RefKind ReturnRefKind { get; init; }

    /// <summary>
    /// Gets the ordered parameter signatures.
    /// </summary>
    public required IReadOnlyList<DotNetParameterSignature> Parameters { get; init; }
}

/// <summary>
/// Creates and matches exact normalized .NET method signatures.
/// </summary>
public static class DotNetMethodSignatures
{
    /// <summary>
    /// Creates the exact original signature behind a direct, constructed, or reduced method.
    /// </summary>
    /// <param name="method">The method to describe.</param>
    /// <returns>The normalized signature.</returns>
    public static DotNetMethodSignature From(IMethodSymbol method)
    {
        var definition = DotNetInvocations.DefinitionOf(method);
        return new()
        {
            ContainingType = definition.ContainingType.OriginalDefinition,
            Name = definition.Name,
            MethodKind = definition.MethodKind,
            IsStatic = definition.IsStatic,
            IsExtensionMethod = definition.IsExtensionMethod,
            GenericArity = definition.Arity,
            ReturnType = definition.ReturnType,
            ReturnRefKind = definition.RefKind,
            Parameters =
            [
                .. definition.Parameters.Select((parameter, index) => new DotNetParameterSignature
                {
                    Type = parameter.Type,
                    RefKind = parameter.RefKind,
                    IsParams = parameter.IsParams,
                    IsExtensionReceiver = definition.IsExtensionMethod && index == 0
                })
            ]
        };
    }

    /// <summary>
    /// Determines whether a candidate has one exact normalized signature.
    /// </summary>
    /// <param name="candidate">The exactly bound candidate method.</param>
    /// <param name="expected">The expected signature.</param>
    /// <returns><see langword="true"/> when every signature dimension matches.</returns>
    public static bool Matches(IMethodSymbol candidate, DotNetMethodSignature expected)
    {
        var actual = From(candidate);
        return SymbolEqualityComparer.IncludeNullability.Equals(actual.ContainingType, expected.ContainingType) &&
            string.Equals(actual.Name, expected.Name, StringComparison.Ordinal) &&
            actual.MethodKind == expected.MethodKind &&
            actual.IsStatic == expected.IsStatic &&
            actual.IsExtensionMethod == expected.IsExtensionMethod &&
            actual.GenericArity == expected.GenericArity &&
            SymbolEqualityComparer.IncludeNullability.Equals(actual.ReturnType, expected.ReturnType) &&
            actual.ReturnRefKind == expected.ReturnRefKind &&
            actual.Parameters.Count == expected.Parameters.Count &&
            actual.Parameters.Zip(expected.Parameters).All(pair =>
                SymbolEqualityComparer.IncludeNullability.Equals(pair.First.Type, pair.Second.Type) &&
                pair.First.RefKind == pair.Second.RefKind &&
                pair.First.IsParams == pair.Second.IsParams &&
                pair.First.IsExtensionReceiver == pair.Second.IsExtensionReceiver);
    }
}
