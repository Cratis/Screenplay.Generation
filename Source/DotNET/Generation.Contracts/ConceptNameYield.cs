// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Generation;

/// <summary>
/// Allows a producer to withdraw its own concept declaration, but only on a same-name conflict.
/// </summary>
public sealed record ConceptNameYieldDefinition
{
    /// <summary>
    /// Gets the diagnostic explaining why the producer omits this declaration on conflict.
    /// </summary>
    public required GenerationDiagnostic Diagnostic { get; init; }
}
