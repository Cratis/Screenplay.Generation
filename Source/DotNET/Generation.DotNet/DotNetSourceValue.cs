// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

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
/// Represents one named value in a bounded payload.
/// </summary>
/// <param name="Name">The formal parameter or member name.</param>
/// <param name="Symbol">The exact formal parameter or initialized member.</param>
/// <param name="Value">The exact bounded value.</param>
/// <param name="Source">The exact authored value location.</param>
public sealed record DotNetNamedValue(
    string Name,
    ISymbol Symbol,
    DotNetSourceValue Value,
    Location Source);

/// <summary>
/// Represents one exact constructed payload.
/// </summary>
/// <param name="Type">The exact payload type.</param>
/// <param name="Values">Constructor values in formal-parameter order followed by initializer values in authored order.</param>
public sealed record DotNetPayloadValue(
    ITypeSymbol Type,
    ImmutableArray<DotNetNamedValue> Values) : DotNetSourceValue;

/// <summary>
/// Represents one exact bounded collection element and its authored location.
/// </summary>
/// <param name="Value">The exact element value.</param>
/// <param name="Source">The exact authored element location.</param>
public sealed record DotNetCollectionElement(
    DotNetSourceValue Value,
    Location Source);

/// <summary>
/// Represents one exact bounded collection.
/// </summary>
/// <param name="Type">The exact collection type when Roslyn supplies one.</param>
/// <param name="Values">The exact elements and source locations in authored order.</param>
public sealed record DotNetCollectionValue(
    ITypeSymbol? Type,
    ImmutableArray<DotNetCollectionElement> Values) : DotNetSourceValue;
