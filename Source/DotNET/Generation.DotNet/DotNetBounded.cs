// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Cratis.Screenplay.Generation.DotNet;

/// <summary>
/// Identifies why bounded .NET source extraction could not prove one exact value.
/// </summary>
public enum DotNetValueFailureKind
{
    /// <summary>
    /// The failure kind is unknown or undefined.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Roslyn could not bind the source exactly.
    /// </summary>
    Unbound = 0,

    /// <summary>
    /// Several symbols or values are possible.
    /// </summary>
    Ambiguous = 1,

    /// <summary>
    /// The source is dynamically bound.
    /// </summary>
    Dynamic = 2,

    /// <summary>
    /// The value is computed by executable code.
    /// </summary>
    Computed = 3,

    /// <summary>
    /// The value depends on a condition.
    /// </summary>
    Conditional = 4,

    /// <summary>
    /// The authored source shape is outside the bounded extractor.
    /// </summary>
    Unsupported = 5,

    /// <summary>
    /// A collection spread hides elements that are not authored individually.
    /// </summary>
    OpaqueSpread = 6,

    /// <summary>
    /// One payload member is assigned more than once.
    /// </summary>
    DuplicateMember = 7
}

/// <summary>
/// Describes one deterministic bounded-extraction failure at its exact syntax location.
/// </summary>
/// <param name="Kind">The failure kind.</param>
/// <param name="Source">The exact source location.</param>
/// <param name="Message">The stable failure message.</param>
public sealed record DotNetValueFailure(
    DotNetValueFailureKind Kind,
    Location Source,
    string Message);

/// <summary>
/// Represents an exact known value or a bounded unknown result.
/// </summary>
/// <typeparam name="T">The extracted value type.</typeparam>
public abstract record DotNetBounded<T>;

/// <summary>
/// Represents one exact known bounded value.
/// </summary>
/// <typeparam name="T">The extracted value type.</typeparam>
/// <param name="Value">The exact value.</param>
public sealed record DotNetKnown<T>(T Value) : DotNetBounded<T>;

/// <summary>
/// Represents a value that could not be proven exactly.
/// </summary>
/// <typeparam name="T">The requested value type.</typeparam>
public sealed record DotNetUnknown<T> : DotNetBounded<T>
{
    ImmutableArray<DotNetValueFailure> _failures = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetUnknown{T}"/> record.
    /// </summary>
    /// <param name="Failures">The deterministic extraction failures.</param>
    public DotNetUnknown(IReadOnlyList<DotNetValueFailure> Failures)
    {
        this.Failures = Failures;
    }

    /// <summary>
    /// Gets or initializes an immutable snapshot of the deterministic extraction failures.
    /// </summary>
    public IReadOnlyList<DotNetValueFailure> Failures
    {
        get => _failures;
        init => _failures = [.. value];
    }

    /// <summary>
    /// Deconstructs the unknown result into its deterministic extraction failures.
    /// </summary>
    /// <param name="failures">The immutable deterministic extraction failures.</param>
    public void Deconstruct(out IReadOnlyList<DotNetValueFailure> failures) => failures = Failures;
}
