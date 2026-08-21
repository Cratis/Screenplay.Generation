// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Represents one semantic assertion contributed by an adapter.
/// </summary>
public abstract record GenerationFact
{
    /// <summary>
    /// Gets the stable identity of this assertion.
    /// </summary>
    public required FactId Id { get; init; }

    /// <summary>
    /// Gets the source-level subject this assertion describes.
    /// </summary>
    public required SubjectId Subject { get; init; }

    /// <summary>
    /// Gets the evidence establishing the assertion.
    /// </summary>
    public required Evidence Evidence { get; init; }
}
