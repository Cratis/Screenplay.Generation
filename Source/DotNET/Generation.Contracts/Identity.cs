// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Identifies an adapter contributing semantic facts.
/// </summary>
public sealed record AdapterIdentity
{
    /// <summary>
    /// Gets the stable adapter identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the adapter version that produced the facts.
    /// </summary>
    public required string Version { get; init; }
}

/// <summary>
/// Identifies one semantic assertion made by an adapter.
/// </summary>
public sealed record FactId
{
    /// <summary>
    /// Gets the stable fact identifier.
    /// </summary>
    public required string Value { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}

/// <summary>
/// Identifies the source-level subject facts describe.
/// </summary>
/// <remarks>
/// A .NET adapter should include project or assembly identity and the fully qualified metadata name. Display names
/// are deliberately not identities because unrelated frameworks and projects regularly use the same short names.
/// </remarks>
public sealed record SubjectId
{
    /// <summary>
    /// Gets the stable subject identifier.
    /// </summary>
    public required string Value { get; init; }

    /// <inheritdoc/>
    public override string ToString() => Value;
}
